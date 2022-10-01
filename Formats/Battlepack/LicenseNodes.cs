using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class LicenseNodes : St2e
    {
        [JsonPropertyName("License Nodes")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public LicenseNodes(Dictionary<string, Entry> entries)
        {
            if (entries.Values.Count > 368)
            {
                throw new ArgumentException("Battlepack Section 12: 'License Nodes' cannot contain more than 368 entries.");
            }

            if (entries.Values.Any(i => i.Contents.Count != 8))
            {
                throw new ArgumentException("Battlepack Section 12: 'Contents' must contain exactly 8 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0x18);
        }

        public LicenseNodes(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry
                {
                    Description = br.ReadUInt16(),
                    Name = br.ReadUInt16(),
                    LpCost = br.ReadByte(),
                    TypeAndIconColor = br.ReadByte(),
                    Restriction = br.ReadByte(),
                    UnlockedByDefaultFor = (PartyMembers)br.ReadByte()
                };

                for (var j = 0; j < 8; j++)
                {
                    entry.Contents.Add(br.ReadUInt16());
                }
                Entries.Add($"License Node {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Description);
                bw.Write(entry.Name);
                bw.Write(entry.LpCost);
                bw.Write(entry.TypeAndIconColor);
                bw.Write(entry.Restriction);
                bw.Write((byte)entry.UnlockedByDefaultFor);

                foreach (var content in entry.Contents)
                {
                    bw.Write(content);
                }
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Name")]
            public ushort Name { get; set; }

            [JsonPropertyName("Description")]
            public ushort Description { get; set; }

            [JsonPropertyName("LP Cost")]
            public byte LpCost { get; set; }

            [JsonPropertyName("Type And Icon Color")]
            public byte TypeAndIconColor { get; set; }

            [JsonPropertyName("Restriction")]
            public byte Restriction { get; set; }

            [JsonPropertyName("Unlocked By Default For")]
            public PartyMembers UnlockedByDefaultFor { get; set; }

            [JsonPropertyName("Contents")]
            public List<ushort> Contents { get; set; }

            public Entry()
            {
                Contents = new List<ushort>();
            }
        }

        [Flags]
        public enum PartyMembers : byte
        {
            None = 0x00,
            Vaan = 0x01,
            Ashe = 0x02,
            Fran = 0x04,
            Balthier = 0x08,
            Basch = 0x10,
            Penelo = 0x20,
            Reks = 0x40
        }
    }
}