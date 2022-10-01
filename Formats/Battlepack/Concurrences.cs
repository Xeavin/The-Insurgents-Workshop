using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Concurrences : St2e
    {
        [JsonPropertyName("Concurrences")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Concurrences(Dictionary<string, Entry> entries)
        {
            if (entries.Count != 16)
            {
                throw new ArgumentException("Battlepack Section 31: 'Concurrences' must contain exactly 16 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0x0D);
        }

        public Concurrences(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.Action = br.ReadByte();

                for (byte j = 1; j < 4; j++)
                {
                    entry.QuickeningLevelTriggerCounts.Add($"Level {j}", br.ReadByte());
                }

                br.BaseStream.Seek(0x09, SeekOrigin.Current);
                Entries.Add($"Concurrence {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Action);
                foreach (var count in entry.QuickeningLevelTriggerCounts.Values)
                {
                    bw.Write(count);
                }
                bw.Write(new byte[9]);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Action")]
            public byte Action { get; set; }

            [JsonPropertyName("Quickening Level Trigger Counts")]
            public Dictionary<string, byte> QuickeningLevelTriggerCounts { get; set; }

            public Entry()
            {
                QuickeningLevelTriggerCounts = new Dictionary<string, byte>();
            }
        }
    }
}