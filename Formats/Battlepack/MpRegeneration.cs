using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class MpRegeneration : St2e
    {
        [JsonPropertyName("MP Regeneration List (By Augment: Martyr, Inquisitor, Warmage)")]
        public Dictionary<string, MpRegByAugment> MpRegByAugmentDic { get; set; }

        [JsonPropertyName("MP Regeneration List (By Foot)")]
        public Dictionary<string, MpRegByFoot> MpRegByFootDic { get; set; }

        [JsonConstructor]
        public MpRegeneration(Dictionary<string, MpRegByAugment> mpRegByAugmentDic, Dictionary<string, MpRegByFoot> mpRegByFootDic)
        {
            if (mpRegByAugmentDic.Count != 20)
            {
                throw new ArgumentException("Battlepack Section 3: 'MP Regeneration List (By Augment: Martyr, Inquisitor, Warmage)' must contain exactly 20 entries.");
            }

            if (mpRegByFootDic == null || mpRegByFootDic.Count == 0)
            {
                throw new ArgumentException("Battlepack Section 3: 'MP Regeneration List (By Foot)' must contain at least 1 entry.");
            }

            if (mpRegByFootDic.Values.Last().MpLimit < 1000)
            {
                throw new ArgumentException("Battlepack Section 3: The last entry of 'MP Regeneration List (By Foot)' must have a 'MP Limit' of at least 999.");
            }

            MpRegByAugmentDic = mpRegByAugmentDic;
            MpRegByFootDic = mpRegByFootDic;
            var entryCount = mpRegByAugmentDic.Count + mpRegByFootDic.Count;
            SetupHeader((uint)entryCount, 0x08);
        }

        public MpRegeneration(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            MpRegByAugmentDic = new Dictionary<string, MpRegByAugment>();
            MpRegByFootDic = new Dictionary<string, MpRegByFoot>();
            for (var i = 0; i < EntryCount; i++)
            {
                if (i < 20)
                {
                    var entry = new MpRegByAugment
                    {
                        RequiredDamage = br.ReadUInt16(),
                        RegeneratedMp = br.ReadByte(),
                        RegeneratedMpSummon = br.ReadByte()
                    };
                    br.BaseStream.Seek(0x04, SeekOrigin.Current);
                    MpRegByAugmentDic.Add($"Entry {i}", entry);
                }
                else
                {
                    var entry = new MpRegByFoot
                    {
                        Steps = br.ReadUInt32(),
                        MpLimit = br.ReadUInt32()
                    };
                    MpRegByFootDic.Add($"Entry {i - 20}", entry);
                }
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in MpRegByAugmentDic.Values)
            {
                bw.Write(entry.RequiredDamage);
                bw.Write(entry.RegeneratedMp);
                bw.Write(entry.RegeneratedMpSummon);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current);
            }

            foreach (var entry in MpRegByFootDic.Values)
            {
                bw.Write(entry.Steps);
                bw.Write(entry.MpLimit);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class MpRegByAugment //martyr, inquisitor, warmage
        {
            [JsonPropertyName("Required Damage")]
            public ushort RequiredDamage { get; set; }

            [JsonPropertyName("Regenerated MP")]
            public byte RegeneratedMp { get; set; }

            [JsonPropertyName("Regenerated MP (Summon)")]
            public byte RegeneratedMpSummon { get; set; }
        }

        public class MpRegByFoot
        {
            [JsonPropertyName("MP Limit")]
            public uint MpLimit { get; set; }

            [JsonPropertyName("Steps")]
            public uint Steps { get; set; }
        }
    }
}