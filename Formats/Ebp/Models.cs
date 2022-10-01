using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Ebp
{
    public class Models
    {
        [JsonPropertyName("Models")]
        public Dictionary<string, int> Entries { get; set; }

        [JsonConstructor]
        public Models(Dictionary<string, int> entries)
        {
            Entries = entries;
        }

        public Models(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            var entryCount = br.ReadUInt32();
            Entries = new Dictionary<string, int>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = br.ReadInt32();
                Entries.Add($"Model {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));

            bw.Write((uint)Entries.Count);
            foreach (var entry in Entries.Values)
            {
                bw.Write(entry);
            }
            BinaryHelper.Align(bw, 16);
        }
    }
}
