using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Ebp
{
    public class Positions
    {
        public static readonly byte[] Magic = { 0x46, 0x46, 0x31, 0x32, 0x50, 0x4F, 0x53, 0x33 }; //FF12POS3

        [JsonPropertyName("Positions")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Positions(Dictionary<string, Entry> entries)
        {
            Entries = entries;
        }

        public Positions(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(8).SequenceEqual(Magic))
            {
                throw new ArgumentException("Ebp Section 16: Unexpected magic.");
            }

            br.BaseStream.Seek(0x04, SeekOrigin.Current); //skip unused offset
            var entryCount = br.ReadUInt32();

            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = new Entry();
                entry.Type = br.ReadSByte();
                br.BaseStream.Seek(0x03, SeekOrigin.Current); //skip 3 unused bytes
                entry.RadiusPosition.X = br.ReadSingle();
                entry.RadiusPosition.Z = br.ReadSingle();
                br.BaseStream.Seek(0x04, SeekOrigin.Current); //skip 4 unused bytes
                entry.SpawnPosition.X = br.ReadSingle();
                entry.SpawnPosition.Y = br.ReadSingle();
                entry.SpawnPosition.Z = br.ReadSingle();
                entry.Direction = br.ReadSingle();
                br.BaseStream.Seek(0x10, SeekOrigin.Current); //skip 16 unused bytes
                Entries.Add($"Position {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(Magic);
            bw.BaseStream.Seek(0x04, SeekOrigin.Current); //skip unused offset
            bw.Write((uint)Entries.Count);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Type);
                bw.BaseStream.Seek(0x03, SeekOrigin.Current); //skip 3 unused bytes
                bw.Write(entry.RadiusPosition.X);
                bw.Write(entry.RadiusPosition.Z);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current); //skip 4 unused bytes
                bw.Write(entry.SpawnPosition.X);
                bw.Write(entry.SpawnPosition.Y);
                bw.Write(entry.SpawnPosition.Z);
                bw.Write(entry.Direction);
                bw.Write(new byte[16]); //skip 16 unused bytes
            }
        }

        public class Entry
        {
            private sbyte type;
            [JsonPropertyName("Type")]
            public sbyte Type
            {
                get => type;
                set
                {
                    if (value > 2)
                    {
                        throw new ArgumentException("Ebp Section 16: 'Type' cannot be higher than 2.");
                    }
                    type = value;
                }
            }

            [JsonPropertyName("Radius Position")]
            public RadiusPosition RadiusPosition { get; set; }

            [JsonPropertyName("Spawn Position")]
            public Vector SpawnPosition { get; set; }

            [JsonPropertyName("Direction (Radian)")]
            public float Direction { get; set; }

            public Entry()
            {
                RadiusPosition = new RadiusPosition();
                SpawnPosition = new Vector();
            }
        }

        public class RadiusPosition
        {
            [JsonPropertyName("X?")]
            public float X { get; set; }

            [JsonPropertyName("Z?")]
            public float Z { get; set; }
        }
    }
}
