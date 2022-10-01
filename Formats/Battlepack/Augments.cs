using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Augments : St2e
    {
        [JsonPropertyName("Augments")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Augments(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x08);
        }

        public Augments(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry
                {
                    EquipmentDescription = br.ReadUInt16(),
                    LicenseDescription = br.ReadUInt16(),
                    Parameter = br.ReadUInt16(),
                    TimerLink = br.ReadUInt16()
                };
                Entries.Add($"Augment {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.EquipmentDescription);
                bw.Write(entry.LicenseDescription);
                bw.Write(entry.Parameter);
                bw.Write(entry.TimerLink);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Equipment Description")]
            public ushort EquipmentDescription { get; set; }

            [JsonPropertyName("License Description")]
            public ushort LicenseDescription { get; set; }

            [JsonPropertyName("Parameter")]
            public ushort Parameter { get; set; }

            private ushort timerLink;

            [JsonPropertyName("Timer Link")]
            public ushort TimerLink
            {
                get => timerLink;
                set
                {
                    if (value > 7 && value != 255)
                    {
                        throw new ArgumentException("Battlepack Section 58: 'Timer Link' must be 255 or lower than 8.");
                    }
                    timerLink = value;
                }
            }
        }
    }
}