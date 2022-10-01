using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class LocationMovementBehaviour : St2e
    {
        [JsonPropertyName("Location Movement Behaviours")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public LocationMovementBehaviour(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x08);
        }

        public LocationMovementBehaviour(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.Location = br.ReadUInt16();
                entry.Range = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                var flags = br.ReadByte();
                entry.IsClose = (flags & 0x01) == 1;
                br.BaseStream.Seek(0x03, SeekOrigin.Current);
                Entries.Add($"Location Movement Behaviour {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                byte flags = 0;
                flags |= entry.IsClose ? (byte)0x01 : (byte)0;

                bw.Write(entry.Location);
                bw.Write(entry.Range);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(flags);
                bw.BaseStream.Seek(0x03, SeekOrigin.Current);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Location")]
            public ushort Location { get; set; }

            [JsonPropertyName("Range?")]
            public byte Range { get; set; }

            [JsonPropertyName("Is Close")]
            public bool IsClose { get; set; }
        }
    }
}