using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class KeyItems : St2e
    {
        [JsonPropertyName("Key Items")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public KeyItems(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x0A);
        }

        public KeyItems(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                entry.Sort = br.ReadUInt16();
                entry.Icon = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.Description = br.ReadUInt16();
                entry.Name = br.ReadUInt16();
                Entries.Add($"Key Item {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                bw.Write(entry.Sort);
                bw.Write(entry.Icon);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
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

            [JsonPropertyName("Icon")]
            public byte Icon { get; set; }

            [JsonPropertyName("Sort")]
            public ushort Sort { get; set; }
        }
    }
}