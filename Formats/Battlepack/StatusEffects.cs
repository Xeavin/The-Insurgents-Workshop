using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class StatusEffects : St2e
    {
        [JsonPropertyName("Status Effects")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public StatusEffects(Dictionary<string, Entry> entries)
        {
            if (entries.Count != 32)
            {
                throw new ArgumentException("Battlepack Section 15: 'Status Effects' must contain exactly 32 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0x28);
        }

        public StatusEffects(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var data = new Entry();
                data.Name = br.ReadUInt16();
                data.DisplayOrderUnknown0 = br.ReadByte();
                br.BaseStream.Seek(0x03, SeekOrigin.Current);
                var flags = br.ReadUInt16();
                data.IsRefreshable = (flags >> 5 & 0x01) == 1;
                data.IsNotApplicableToFoes = (flags >> 8 & 0x01) == 1;
                data.IsNullifiedByRemedy = (flags >> 10 & 0x01) == 1;
                data.IsNullifiedByRemedyLore1 = (flags >> 11 & 0x01) == 1;
                data.IsNullifiedByRemedyLore2 = (flags >> 12 & 0x01) == 1;
                data.IsNegative = (flags >> 13 & 0x01) == 1;
                data.IsNullifiedByRemedyLore3 = (flags >> 14 & 0x01) == 1;
                data.Duration = br.ReadByte();
                data.Ticks = br.ReadByte();
                data.Description = br.ReadUInt16();
                data.NullifiedStatusEffects = (StatusEffectsEnum)br.ReadUInt32();
                data.DisplayOrderMenu = br.ReadByte();
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                data.DisplayOrderUnknown1 = br.ReadByte();
                br.BaseStream.Seek(0x0C, SeekOrigin.Current);
                data.RequiredAbsentStatusEffects = (StatusEffectsEnum)br.ReadUInt32();
                data.UnknownUiFlags = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                data.DisplayOrderBattleStatusBar = br.ReadByte();
                data.DisplayOrderTargetInfo = br.ReadByte();
                Entries.Add(((StatusEffectsEnum)(1 << i)).ToString(), data);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                ushort flags = 0;
                flags |= entry.IsRefreshable ? (ushort)0x0020 : (ushort)0;
                flags |= entry.IsNotApplicableToFoes ? (ushort)0x0100 : (ushort)0;
                flags |= entry.IsNullifiedByRemedy ? (ushort)0x0400 : (ushort)0;
                flags |= entry.IsNullifiedByRemedyLore1 ? (ushort)0x0800 : (ushort)0;
                flags |= entry.IsNullifiedByRemedyLore2 ? (ushort)0x1000 : (ushort)0;
                flags |= entry.IsNegative ? (ushort)0x2000 : (ushort)0;
                flags |= entry.IsNullifiedByRemedyLore3 ? (ushort)0x4000 : (ushort)0;

                bw.Write(entry.Name);
                bw.Write(entry.DisplayOrderUnknown0);
                bw.BaseStream.Seek(0x03, SeekOrigin.Current);
                bw.Write(flags);
                bw.Write(entry.Duration);
                bw.Write(entry.Ticks);
                bw.Write(entry.Description);
                bw.Write((uint)entry.NullifiedStatusEffects);
                bw.Write(entry.DisplayOrderMenu);
                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                bw.Write(entry.DisplayOrderUnknown1);
                bw.BaseStream.Seek(0x0C, SeekOrigin.Current);
                bw.Write((uint)entry.RequiredAbsentStatusEffects);
                bw.Write(entry.UnknownUiFlags);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.DisplayOrderBattleStatusBar);
                bw.Write(entry.DisplayOrderTargetInfo);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Name")]
            public ushort Name { get; set; }

            [JsonPropertyName("Description")]
            public ushort Description { get; set; }

            [JsonPropertyName("Duration")]
            public byte Duration { get; set; }

            [JsonPropertyName("Ticks")]
            public byte Ticks { get; set; }

            [JsonPropertyName("Nullified Status Effects")]
            public StatusEffectsEnum NullifiedStatusEffects { get; set; }

            [JsonPropertyName("Required Absent Status Effects")]
            public StatusEffectsEnum RequiredAbsentStatusEffects { get; set; }

            [JsonPropertyName("Is Negative")]
            public bool IsNegative { get; set; }

            [JsonPropertyName("Is Refreshable")]
            public bool IsRefreshable { get; set; }

            [JsonPropertyName("Is Not Applicable To Foes")]
            public bool IsNotApplicableToFoes { get; set; }

            [JsonPropertyName("Is Nullified By Remedy")]
            public bool IsNullifiedByRemedy { get; set; }

            [JsonPropertyName("Is Nullified By Remedy Lore 1")]
            public bool IsNullifiedByRemedyLore1 { get; set; }

            [JsonPropertyName("Is Nullified By Remedy Lore 2")]
            public bool IsNullifiedByRemedyLore2 { get; set; }

            [JsonPropertyName("Is Nullified By Remedy Lore 3")]
            public bool IsNullifiedByRemedyLore3 { get; set; }

            [JsonPropertyName("Display Order (Unknown 0)")]
            public byte DisplayOrderUnknown0 { get; set; }

            [JsonPropertyName("Display Order (Menu)")]
            public byte DisplayOrderMenu { get; set; }

            [JsonPropertyName("Display Order (Unknown 1)")]
            public byte DisplayOrderUnknown1 { get; set; }

            [JsonPropertyName("Display Order (Battle Status Bar)")]
            public byte DisplayOrderBattleStatusBar { get; set; }

            [JsonPropertyName("Display Order (Target Info)")]
            public byte DisplayOrderTargetInfo { get; set; }

            [JsonPropertyName("Unknown UI Flags")]
            public byte UnknownUiFlags { get; set; }
        }
    }
}