using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Ard
{
    public class Models
    {
        [JsonPropertyName("Models")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Models(Dictionary<string, Entry> entries)
        {
            Entries = entries;
        }
        public Models(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            var entryCount = br.ReadUInt32();
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = new Entry
                {
                    Model = br.ReadInt32(),
                    UnknownFlags = br.ReadUInt32()
                };
                Entries.Add($"Model {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));

            bw.Write(Entries.Count);
            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Model);
                bw.Write(entry.UnknownFlags);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Model")]
            public int Model { get; set; }

            [JsonPropertyName("Unknown Flags")]
            public uint UnknownFlags { get; set; }
        }
    }
}
