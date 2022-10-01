using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Loot : St2e
    {
        [JsonPropertyName("Loot List")]
        public Dictionary<string, Entry> Entries { get; set; }


        [JsonConstructor]
        public Loot(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x0A);
        }

        public Loot(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var data = new Entry();
                data.GilCost = br.ReadUInt16();
                data.Description = br.ReadUInt16();
                data.Icon = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                data.Name = br.ReadUInt16();
                data.Sort = br.ReadUInt16();
                Entries.Add($"Loot {i}", data);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.GilCost);
                bw.Write(entry.Description);
                bw.Write(entry.Icon);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.Name);
                bw.Write(entry.Sort);
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
            public ushort GilCost { get; set; }

            [JsonPropertyName("Icon")]
            public byte Icon { get; set; }

            [JsonPropertyName("Sort")]
            public ushort Sort { get; set; }
        }
    }
}