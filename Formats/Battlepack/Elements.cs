using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Elements : St2e
    {
        [JsonPropertyName("Elements")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Elements(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x02);
        }

        public Elements(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry
                {
                    Name = br.ReadUInt16()
                };
                Entries.Add(((ElementsEnum)(1 << i)).ToString(), entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Name);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Name")]
            public ushort Name { get; set; }
        }
    }
}