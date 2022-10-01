using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Items : St2e
    {
        [JsonPropertyName("Items")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Items(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x0C);
        }

        public Items(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.Sort = br.ReadByte();
                br.BaseStream.Seek(0x03, SeekOrigin.Current);
                entry.Icon = br.ReadByte();
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                entry.ActionDescriptionLink = br.ReadByte();
                entry.ActionNameLink = br.ReadUInt16();
                entry.GilCost = br.ReadUInt16();
                Entries.Add($"Item {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Sort);
                bw.BaseStream.Seek(0x03, SeekOrigin.Current);
                bw.Write(entry.Icon);
                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                bw.Write(entry.ActionDescriptionLink);
                bw.Write(entry.ActionNameLink);
                bw.Write(entry.GilCost);
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