using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Technicks : St2e
    {
        [JsonPropertyName("Techicks")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Technicks(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x08);
        }

        public Technicks(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.GilCost = br.ReadUInt16();
                entry.ActionDescriptionLink = br.ReadByte();
                entry.Icon = br.ReadByte();
                entry.ActionNameLink = br.ReadUInt16();
                entry.Sort = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                Entries.Add($"Technick {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.GilCost);
                bw.Write(entry.ActionDescriptionLink);
                bw.Write(entry.Icon);
                bw.Write(entry.ActionNameLink);
                bw.Write(entry.Sort);
                bw.Write((byte)0x00);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Action Name Link")]
            public ushort ActionNameLink { get; set; }

            [JsonPropertyName("Action Description Link")]
            public byte ActionDescriptionLink { get; set; }

            [JsonPropertyName("Gil Cost")]
            public ushort GilCost { get; set; }

            [JsonPropertyName("Icon")]
            public byte Icon { get; set; }

            [JsonPropertyName("Sort")]
            public byte Sort { get; set; }
        }
    }
}