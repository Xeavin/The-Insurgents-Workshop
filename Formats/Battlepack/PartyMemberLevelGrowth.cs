using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class PartyMemberLevelGrowth : St2e
    {
        [JsonPropertyName("Party Member Level Growth")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public PartyMemberLevelGrowth(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x02);
        }

        public PartyMemberLevelGrowth(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 1; i <= EntryCount; i++)
            {
                var entry = new Entry
                {
                    Hp = br.ReadByte(),
                    Mp = br.ReadByte()
                };
                Entries.Add($"Level {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Hp);
                bw.Write(entry.Mp);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("HP")]
            public byte Hp { get; set; }

            [JsonPropertyName("MP")]
            public byte Mp { get; set; }
        }
    }
}