using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class InitialInventory : St2e
    {
        [JsonPropertyName("Initial Inventory")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public InitialInventory(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x04);
        }

        public InitialInventory(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry
                {
                    Content = br.ReadUInt16(),
                    Quantity = br.ReadUInt16()
                };
                Entries.Add($"Inventory {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Content);
                bw.Write(entry.Quantity);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Content")]
            public ushort Content { get; set; }

            [JsonPropertyName("Quantity")]
            public ushort Quantity { get; set; }
        }
    }
}