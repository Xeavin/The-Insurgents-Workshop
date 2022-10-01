using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class BazaarGoods : St2e
    {
        [JsonPropertyName("Bazaar Goods")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public BazaarGoods(Dictionary<string, Entry> entries)
        {
            if (entries.Values.Any(i => i.Packages.Count != 3))
            {
                throw new ArgumentException("Battlepack Section 57: 'Packages' must contain exactly 3 entries.");
            }

            if (entries.Values.Any(i => i.Ingredients.Count != 3))
            {
                throw new ArgumentException("Battlepack Section 57: 'Ingredients' must contain exactly 3 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0x24);
        }

        public BazaarGoods(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.Name = br.ReadUInt16();
                entry.Description = br.ReadUInt16();

                for (var j = 0; j < 3; j++)
                {
                    var inventory = new Inventory
                    {
                        Content = br.ReadUInt16(),
                        Quantity = br.ReadUInt16()
                    };
                    entry.Packages.Add($"Content {j}", inventory);
                }

                entry.GilCost = br.ReadUInt32();
                var flags = br.ReadUInt16();
                entry.Type = (byte)(flags & 0x03);

                for (var j = 0; j < 3; j++)
                {
                    var inventory = new Inventory
                    {
                        Content = br.ReadUInt16(),
                        Quantity = br.ReadUInt16()
                    };
                    entry.Ingredients.Add($"Ingredient {j}", inventory);
                }

                entry.Icon = br.ReadUInt16();
                Entries.Add($"Bazaar Good {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                ushort flags = 0;
                flags |= entry.Type;
                bw.Write(entry.Name);
                bw.Write(entry.Description);

                foreach (var inventory in entry.Packages.Values)
                {
                    bw.Write(inventory.Content);
                    bw.Write(inventory.Quantity);
                }

                bw.Write(entry.GilCost);
                bw.Write(flags);

                foreach (var inventory in entry.Ingredients.Values)
                {
                    bw.Write(inventory.Content);
                    bw.Write(inventory.Quantity);
                }

                bw.Write(entry.Icon);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Name")]
            public ushort Name { get; set; }

            [JsonPropertyName("Description")]
            public ushort Description { get; set; }

            [JsonPropertyName("Gil Cost")]
            public uint GilCost { get; set; }

            [JsonPropertyName("Icon")]
            public ushort Icon { get; set; }

            private byte type;

            [JsonPropertyName("Type")]
            public byte Type
            {
                get => type;
                set
                {
                    if (value > 2)
                    {
                        throw new ArgumentException("Battlepack Section 57: 'Type' cannot be higher than 2.");
                    }
                    type = value;
                }
            }

            [JsonPropertyName("Packages")]
            public Dictionary<string, Inventory> Packages { get; set; }

            [JsonPropertyName("Ingredients")]
            public Dictionary<string, Inventory> Ingredients { get; set; }

            public Entry()
            {
                Packages = new Dictionary<string, Inventory>();
                Ingredients = new Dictionary<string, Inventory>();
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