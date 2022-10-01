using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class WeaponStances
    {
        [JsonPropertyName("Weapon Stances")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public WeaponStances(Dictionary<string, Entry> entries)
        {
            Entries = entries;
        }

        public WeaponStances(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            br.BaseStream.Seek(0x02, SeekOrigin.Begin); //skip entry size
            var entryCount = br.ReadUInt16();

            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = new Entry
                {
                    Animation = br.ReadByte()
                };
                Entries.Add($"Weapon Stance {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(Entry.Size);
            bw.Write((ushort)Entries.Count);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Animation);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            public static readonly ushort Size = 0x01;

            [JsonPropertyName("Animation")]
            public byte Animation { get; set; }
        }
    }
}
