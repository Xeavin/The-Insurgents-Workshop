using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Packages : St2e
    {
        [JsonPropertyName("Packages")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Packages(Dictionary<string, Entry> entries)
        {
            if (entries.Values.Any(i => i.Contents.Count != 2))
            {
                throw new ArgumentException("Battlepack Section 37: 'Contents' must contain exactly 2 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0x0C);
        }

        public Packages(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry
                {
                    Gil = br.ReadUInt32()
                };

                for (var j = 0; j < 2; j++)
                {
                    var inventory = new Inventory
                    {
                        Content = br.ReadUInt16(),
                        Quantity = br.ReadUInt16()
                    };
                    entry.Contents.Add($"Content {j}", inventory);
                }
                Entries.Add($"Package {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Gil);
                foreach (var inventory in entry.Contents.Values)
                {
                    bw.Write(inventory.Content);
                    bw.Write(inventory.Quantity);
                }
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Gil")]
            public uint Gil { get; set; }

            [JsonPropertyName("Contents")]
            public Dictionary<string, Inventory> Contents { get; set; }

            public Entry()
            {
                Contents = new Dictionary<string, Inventory>();
            }
        }

        public class Inventory
        {
            [JsonPropertyName("Content")]
            public ushort Content { get; set; }

            [JsonPropertyName("Quantity")]
            public ushort Quantity { get; set; }
        }
    }
}