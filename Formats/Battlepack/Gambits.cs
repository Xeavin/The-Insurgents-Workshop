using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Gambits : St2e
    {
        [JsonPropertyName("Gambits")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Gambits(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x20);
        }

        public Gambits(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                br.BaseStream.Seek(0x03, SeekOrigin.Current);
                entry.Icon = br.ReadByte();
                entry.Description = br.ReadUInt16();
                entry.GilCost = br.ReadUInt16();
                entry.FirstCase.TargetCondition = br.ReadUInt16();
                entry.SecondCase.TargetCondition = br.ReadUInt16();
                entry.ThirdCase.TargetCondition = br.ReadUInt16();
                entry.Flags = br.ReadUInt16();
                entry.FirstCase.TargetType = br.ReadByte();
                entry.SecondCase.TargetType = br.ReadByte();
                entry.ThirdCase.TargetType = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.Name = br.ReadUInt16();
                entry.GambitPage = br.ReadByte();
                entry.GambitPageOrder = br.ReadByte();
                entry.FirstCase.Parameter = br.ReadUInt16();
                entry.SecondCase.Parameter = br.ReadUInt16();
                entry.ThirdCase.Parameter = br.ReadUInt16();
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                Entries.Add($"Gambit {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.BaseStream.Seek(0x03, SeekOrigin.Current);
                bw.Write(entry.Icon);
                bw.Write(entry.Description);
                bw.Write(entry.GilCost);
                bw.Write(entry.FirstCase.TargetCondition);
                bw.Write(entry.SecondCase.TargetCondition);
                bw.Write(entry.ThirdCase.TargetCondition);
                bw.Write(entry.Flags);
                bw.Write(entry.FirstCase.TargetType);
                bw.Write(entry.SecondCase.TargetType);
                bw.Write(entry.ThirdCase.TargetType);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.Name);
                bw.Write(entry.GambitPage);
                bw.Write(entry.GambitPageOrder);
                bw.Write(entry.FirstCase.Parameter);
                bw.Write(entry.SecondCase.Parameter);
                bw.Write(entry.ThirdCase.Parameter);
                bw.Write(new byte[2]);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Name")]
            public ushort Name { get; set; }

            [JsonPropertyName("Description")]
            public ushort Description { get; set; }

            [JsonPropertyName("Gil Cost")]
            public ushort GilCost { get; set; }

            [JsonPropertyName("Icon (Ineffectual)")]
            public byte Icon { get; set; }

            [JsonPropertyName("Gambit Page")]
            public byte GambitPage { get; set; }

            [JsonPropertyName("Gambit Page Order")]
            public byte GambitPageOrder { get; set; }

            [JsonPropertyName("Flags")]
            public ushort Flags { get; set; }

            [JsonPropertyName("1. Case")]
            public Case FirstCase { get; set; }

            [JsonPropertyName("2. Case")]
            public Case SecondCase { get; set; }

            [JsonPropertyName("3. Case")]
            public Case ThirdCase { get; set; }

            public Entry()
            {
                FirstCase = new Case();
                SecondCase = new Case();
                ThirdCase = new Case();
            }
        }

        public class Case
        {
            [JsonPropertyName("Target Type")]
            public byte TargetType { get; set; }

            [JsonPropertyName("Target Condition")]
            public ushort TargetCondition { get; set; }

            [JsonPropertyName("Parameter")]
            public ushort Parameter { get; set; }
        }
    }
}