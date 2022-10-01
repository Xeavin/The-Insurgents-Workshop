using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Mist : St2e
    {
        [JsonPropertyName("Mist List")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Mist(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x08);
        }

        public Mist(string filename)
        {
            Entries = new Dictionary<string, Entry>();
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry
                {
                    ActionNameLink = br.ReadUInt16(),
                    ActionDescriptionLink = br.ReadUInt16()
                };
                br.BaseStream.Seek(0x04, SeekOrigin.Current);
                Entries.Add($"Mist {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.ActionNameLink);
                bw.Write(entry.ActionDescriptionLink);
                bw.Write(new byte[4]);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Action Name Link")]
            public ushort ActionNameLink { get; set; }

            [JsonPropertyName("Action Description Link")]
            public ushort ActionDescriptionLink { get; set; }
        }
    }
}