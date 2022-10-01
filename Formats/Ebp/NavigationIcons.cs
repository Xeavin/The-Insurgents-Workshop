using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Ebp
{
    public class NavigationIcons
    {
        public static readonly byte[] Magic = { 0x4E, 0x41, 0x56, 0x49, 0x49, 0x43, 0x4E, 0x32 }; //NAVIICN2

        [JsonPropertyName("Navigation Icons")]
        public Dictionary<string, Group> Groups { get; set; }

        [JsonConstructor]
        public NavigationIcons(Dictionary<string, Group> groups)
        {
            Groups = groups;
        }

        public NavigationIcons(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(8).SequenceEqual(Magic))
            {
                throw new ArgumentException("Ebp Section 4: Unexpected magic.");
            }

            var groupCount = br.ReadUInt16();
            br.BaseStream.Seek(0x0A, SeekOrigin.Begin); //skip header

            var groupOffsets = new ushort[groupCount];
            var groupEntryCounts = new ushort[groupCount];
            for (var i = 0; i < groupCount; i++)
            {
                groupOffsets[i] = br.ReadUInt16();
                groupEntryCounts[i] = br.ReadUInt16();
            }

            Groups = new Dictionary<string, Group>();
            for (var i = 0; i < groupCount; i++)
            {
                var group = new Group();
                br.BaseStream.Seek(groupOffsets[i], SeekOrigin.Begin);
                for (var j = 0; j < groupEntryCounts[i]; j++)
                {
                    var entry = new Entry
                    {
                        LinePositionX = br.ReadInt16(),
                        LinePositionY = br.ReadInt16(),
                        IconPositionX = br.ReadInt16(),
                        IconPositionY = br.ReadInt16(),
                        IconLink = br.ReadUInt16(),
                        RequiredMapFlag = br.ReadUInt16(),
                        Unused0 = br.ReadBytes(4)
                    };
                    group.Entries.Add($"Entry {j}", entry);
                }
                Groups.Add($"Group {i}", group);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(Magic);
            bw.Write((uint)Groups.Count);

            //reserve space for label offsets and links
            bw.BaseStream.Seek(0x0A + Groups.Count * 0x04, SeekOrigin.Begin); //0x0A for header, x*0x04 for x groups.

            var groupOffsets = new ushort[Groups.Count];
            var groupEntryCounts = new ushort[Groups.Count];
            for (var i = 0; i < Groups.Count; i++)
            {
                var group = Groups.Values.ElementAt(i);
                groupOffsets[i] = group.Entries.Count != 0 ? (ushort)bw.BaseStream.Position : (ushort)0;
                groupEntryCounts[i] = (ushort)group.Entries.Count;

                foreach (var entry in group.Entries.Values)
                {
                    bw.Write(entry.LinePositionX);
                    bw.Write(entry.LinePositionY);
                    bw.Write(entry.IconPositionX);
                    bw.Write(entry.IconPositionY);
                    bw.Write(entry.IconLink);
                    bw.Write(entry.RequiredMapFlag);
                    bw.Write(entry.Unused0);
                }
            }
            BinaryHelper.Align(bw, 16);

            //write group offsets and its entry counts
            bw.BaseStream.Seek(0x0A, SeekOrigin.Begin);
            for (var i = 0; i < Groups.Count; i++)
            {
                bw.Write(groupOffsets[i]);
                bw.Write(groupEntryCounts[i]);
            }
        }

        public class Group
        {
            [JsonPropertyName("Entries")]
            public Dictionary<string, Entry> Entries { get; set; }

            public Group()
            {
                Entries = new Dictionary<string, Entry>();
            }

            [JsonConstructor]
            public Group(Dictionary<string, Entry> entries)
            {
                if (entries.Values.Any(i => i.Unused0.Length != 4))
                {
                    throw new ArgumentException("Ebp Section 4: 'Unused 0' must have exactly 4 entries.");
                }

                Entries = entries;
            }
        }

        public class Entry
        {
            [JsonPropertyName("Line Position X")]
            public short LinePositionX { get; set; }

            [JsonPropertyName("Line Position Y")]
            public short LinePositionY { get; set; }

            [JsonPropertyName("Icon Position X")]
            public short IconPositionX { get; set; }

            [JsonPropertyName("Icon Position Y")]
            public short IconPositionY { get; set; }

            [JsonPropertyName("Icon Link")]
            public ushort IconLink { get; set; }

            [JsonPropertyName("Required Map Flag")]
            public ushort RequiredMapFlag { get; set; }

            [JsonPropertyName("Unused 0?")]
            public byte[] Unused0 { get; set; } //count: 4

            public Entry()
            {
                Unused0 = new byte[4];
            }
        }
    }
}
