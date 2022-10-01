using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class ActionGroups
    {
        [JsonPropertyName("Groups")]
        public Dictionary<string, Group> Groups { get; set; }

        [JsonConstructor]
        public ActionGroups(Dictionary<string, Group> groups)
        {
            Groups = groups;
        }

        public ActionGroups(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            var groupCount = br.ReadUInt32() + 1; //+1 because of end of file offset
            var groupOffsets = new List<uint>();
            for (var i = 0; i < groupCount; i++)
            {
                groupOffsets.Add(br.ReadUInt32());
            }

            br.BaseStream.Seek(groupOffsets[0], SeekOrigin.Begin);
            Groups = new Dictionary<string, Group>();
            for (var i = 0; i < groupCount - 1; i++)
            {
                var group = new Group();
                var entryCount = (groupOffsets[i + 1] - groupOffsets[i]) / 4;

                for (var j = 0; j < entryCount; j++)
                {
                    var entry = new Entry
                    {
                        Action = br.ReadUInt16(),
                        Chance = br.ReadUInt16()
                    };
                    group.Entries.Add($"Entry {j}", entry);
                }
                Groups.Add($"Group {i}", group);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            var count = (uint)Groups.Count;
            bw.Write(count);

            bw.BaseStream.Seek(((int)count + 1) * 0x04, SeekOrigin.Current); //+1 because of end of file offset
            BinaryHelper.Align(bw, 16);

            var groupOffsets = new List<uint>();
            foreach (var group in Groups.Values)
            {
                groupOffsets.Add((uint)bw.BaseStream.Position);
                foreach (var entry in group.Entries.Values)
                {
                    bw.Write(entry.Action);
                    bw.Write(entry.Chance);
                }
            }
            var endOfFileOffset = (uint)bw.BaseStream.Position; //without alignment
            BinaryHelper.Align(bw, 16);

            //write script offsets and end of file offset
            bw.BaseStream.Seek(0x04, SeekOrigin.Begin);
            foreach (var offset in groupOffsets)
            {
                bw.Write(offset);
            }
            bw.Write(endOfFileOffset);
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
                Entries = entries;
            }
        }

        public class Entry
        {
            [JsonPropertyName("Action")]
            public ushort Action { get; set; }

            [JsonPropertyName("Chance")]
            public ushort Chance { get; set; }
        }
    }
}
