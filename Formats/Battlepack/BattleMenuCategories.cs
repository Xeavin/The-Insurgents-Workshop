using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class BattleMenuCategories : St2e
    {
        [JsonPropertyName("Battle Menu Categories")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public BattleMenuCategories(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x04);
        }

        public BattleMenuCategories(string filename)
        {
            Entries = new Dictionary<string, Entry>();
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry
                {
                    Description = br.ReadUInt16(),
                    Name = br.ReadUInt16()
                };
                Entries.Add($"Battle Menu Category {i}", entry);
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
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Name")]
            public ushort Name { get; set; }

            [JsonPropertyName("Description")]
            public ushort Description { get; set; }
        }
    }
}