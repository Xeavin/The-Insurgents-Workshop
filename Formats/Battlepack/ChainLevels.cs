using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class ChainLevels : St2e
    {
        [JsonPropertyName("Chain Levels")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public ChainLevels(Dictionary<string, Entry> entries)
        {
            if (entries.Count != 4)
            {
                throw new ArgumentException("Battlepack Section 6: 'Chain Levels' must contain exactly 4 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0x25);
        }

        public ChainLevels(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry
                {
                    CommonDropRate = br.ReadSByte(),
                    UncommonDropRate = br.ReadSByte(),
                    RareDropRate = br.ReadSByte(),
                    VeryRareDropRate = br.ReadSByte(),
                    GuaranteedDropRate = br.ReadSByte(),
                    IdenticalFoeConditionCount = br.ReadByte()
                };

                for (var j = 1; j < 5; j++)
                {
                    entry.CommonAmountRates.Add(j, br.ReadSByte());
                }
                for (var j = 1; j < 5; j++)
                {
                    entry.UncommonAmountRates.Add(j, br.ReadSByte());
                }
                for (var j = 1; j < 5; j++)
                {
                    entry.RareAmountRates.Add(j, br.ReadSByte());
                }
                for (var j = 1; j < 5; j++)
                {
                    entry.VeryRareAmountRates.Add(j, br.ReadSByte());
                }
                for (var j = 1; j < 5; j++)
                {
                    entry.GuaranteedAmountRates.Add(j, br.ReadSByte());
                }
                for (var j = 0; j < 7; j++)
                {
                    entry.BenefitRates.Add(BenefitRateOptions[j], br.ReadByte());
                }

                entry.ReverseChainDifferentFoeConditionCount = br.ReadByte();
                for (var j = 1; j < 4; j++)
                {
                    entry.ReverseChainAmountRates.Add(j, br.ReadSByte());
                }
                Entries.Add($"Chain Level {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.CommonDropRate);
                bw.Write(entry.UncommonDropRate);
                bw.Write(entry.RareDropRate);
                bw.Write(entry.VeryRareDropRate);
                bw.Write(entry.GuaranteedDropRate);
                bw.Write(entry.IdenticalFoeConditionCount);

                foreach (var rate in entry.CommonAmountRates.Values)
                {
                    bw.Write(rate);
                }
                foreach (var rate in entry.UncommonAmountRates.Values)
                {
                    bw.Write(rate);
                }
                foreach (var rate in entry.RareAmountRates.Values)
                {
                    bw.Write(rate);
                }
                foreach (var rate in entry.VeryRareAmountRates.Values)
                {
                    bw.Write(rate);
                }
                foreach (var rate in entry.GuaranteedAmountRates.Values)
                {
                    bw.Write(rate);
                }
                foreach (var rate in entry.BenefitRates.Values)
                {
                    bw.Write(rate);
                }

                bw.Write(entry.ReverseChainDifferentFoeConditionCount);
                foreach (var rate in entry.ReverseChainAmountRates.Values)
                {
                    bw.Write(rate);
                }
            }

            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Identical Foe Condition Count")]
            public byte IdenticalFoeConditionCount { get; set; }

            [JsonPropertyName("Common Drop Rate")]
            public sbyte CommonDropRate { get; set; }

            [JsonPropertyName("Common Amount Rates")]
            public Dictionary<int, sbyte> CommonAmountRates { get; set; }

            [JsonPropertyName("Uncommon Drop Rate")]
            public sbyte UncommonDropRate { get; set; }

            [JsonPropertyName("Uncommon Amount Rates")]
            public Dictionary<int, sbyte> UncommonAmountRates { get; set; }

            [JsonPropertyName("Rare Drop Rate")] public sbyte RareDropRate { get; set; }

            [JsonPropertyName("Rare Amount Rates")]
            public Dictionary<int, sbyte> RareAmountRates { get; set; }

            [JsonPropertyName("Very Rare Drop Rate")]
            public sbyte VeryRareDropRate { get; set; }

            [JsonPropertyName("Very Rare Amount Rates")]
            public Dictionary<int, sbyte> VeryRareAmountRates { get; set; }

            [JsonPropertyName("Guaranteed Drop Rate")]
            public sbyte GuaranteedDropRate { get; set; }

            [JsonPropertyName("Guaranteed Amount Rates")]
            public Dictionary<int, sbyte> GuaranteedAmountRates { get; set; }

            [JsonPropertyName("Benefit Rates")]
            public Dictionary<string, byte> BenefitRates { get; set; }

            [JsonPropertyName("Reverse Chain - Different Foe Condition Count")]
            public byte ReverseChainDifferentFoeConditionCount { get; set; }

            [JsonPropertyName("Reverse Chain - Amount Rates")]
            public Dictionary<int, sbyte> ReverseChainAmountRates { get; set; }

            public Entry()
            {
                CommonAmountRates = new Dictionary<int, sbyte>();
                UncommonAmountRates = new Dictionary<int, sbyte>();
                RareAmountRates = new Dictionary<int, sbyte>();
                VeryRareAmountRates = new Dictionary<int, sbyte>();
                GuaranteedAmountRates = new Dictionary<int, sbyte>();
                BenefitRates = new Dictionary<string, byte>();
                ReverseChainAmountRates = new Dictionary<int, sbyte>();
            }
        }

        private static readonly List<string> BenefitRateOptions = new()
        {
            "None",
            "HP Recovery",
            "MP Recovery",
            "Leader Protect",
            "Leader Shell",
            "All Protect",
            "All Shell"
        };
    }
}