using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Ard
{
    public class Classes : St2e
    {
        [JsonPropertyName("Classes")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Classes(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x54);
        }

        public Classes(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            ReadHeader(br);
            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.Model = br.ReadInt32();
                entry.Classification = br.ReadByte();
                entry.Genus = br.ReadByte();
                entry.Unknown0 = br.ReadUInt16();
                br.BaseStream.Seek(0x08, SeekOrigin.Current);
                entry.Weight = br.ReadInt16();
                var flags = br.ReadByte();
                entry.ChargeAuraAnimationScale = (byte)(flags & 0x07);
                entry.UnknownFlag0 = (flags >> 4 & 0x01) == 1;
                entry.UnknownFlag1 = (flags >> 5 & 0x01) == 1;
                entry.HasCollision = (flags >> 6 & 0x01) == 1;
                entry.HasFlyingInfo = (flags >> 7 & 0x01) == 1;
                br.BaseStream.Seek(0x05, SeekOrigin.Current);
                flags = br.ReadByte(); //second flags
                entry.UnknownFlag2 = (byte)(flags >> 1 & 0x07);
                entry.IsFlying = (flags >> 4 & 0x01) == 1;
                entry.IsFloating = (flags >> 5 & 0x01) == 1;
                entry.UseTeleportAttack = (flags >> 6 & 0x01) == 1;
                br.BaseStream.Seek(0x07, SeekOrigin.Current);
                entry.MaxComboHits = br.ReadByte();
                flags = br.ReadByte(); //third flags
                entry.UseDistancedAttack = (flags >> 1 & 0x01) == 1;
                entry.AngleDetection = br.ReadByte();
                entry.RadiusDetection = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.SoundAndMagickDetection = br.ReadByte();
                entry.LifeDetection = br.ReadByte();
                entry.Unknown1 = br.ReadByte();
                flags = br.ReadByte(); //fourth flags
                entry.UnknownFlag3 = (flags & 0x01) == 1;
                entry.IgnoreChain = (flags >> 1 & 0x01) == 1;
                entry.ElementalAffinitiesAbsorb = (ElementsEnum)br.ReadByte();
                entry.ElementalAffinitiesHalfDamage = (ElementsEnum)br.ReadByte();
                entry.ElementalAffinitiesImmune = (ElementsEnum)br.ReadByte();
                entry.ElementalAffinitiesWeak = (ElementsEnum)br.ReadByte();
                entry.ElementalAffinitiesPotency = (ElementsEnum)br.ReadByte();
                br.BaseStream.Seek(0x12, SeekOrigin.Current); //2 unused bytes + 2*8 unused referenced actions
                entry.ChainIdentifier = br.ReadUInt16();
                br.BaseStream.Seek(0x10, SeekOrigin.Current);
                entry.BestiaryIdentifier = br.ReadUInt16();
                Entries.Add($"Class {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                byte firstFlags = 0, secondFlags = 0, thirdFlags = 0, fourthFlags = 0;

                firstFlags |= entry.ChargeAuraAnimationScale;
                firstFlags |= entry.UnknownFlag0 ? (byte)0x10 : (byte)0;
                firstFlags |= entry.UnknownFlag1 ? (byte)0x20 : (byte)0;
                firstFlags |= entry.HasCollision ? (byte)0x40 : (byte)0;
                firstFlags |= entry.HasFlyingInfo ? (byte)0x80 : (byte)0;
                secondFlags |= (byte)(entry.UnknownFlag2 << 1);
                secondFlags |= entry.IsFlying ? (byte)0x10 : (byte)0;
                secondFlags |= entry.IsFloating ? (byte)0x20 : (byte)0;
                secondFlags |= entry.UseTeleportAttack ? (byte)0x40 : (byte)0;
                thirdFlags |= entry.UseDistancedAttack ? (byte)0x02 : (byte)0;
                fourthFlags |= entry.UnknownFlag3 ? (byte)0x01 : (byte)0;
                fourthFlags |= entry.IgnoreChain ? (byte)0x02 : (byte)0;

                bw.Write(entry.Model);
                bw.Write(entry.Classification);
                bw.Write(entry.Genus);
                bw.Write(entry.Unknown0);
                bw.BaseStream.Seek(0x08, SeekOrigin.Current);
                bw.Write(entry.Weight);
                bw.Write(firstFlags);
                bw.BaseStream.Seek(0x05, SeekOrigin.Current);
                bw.Write(secondFlags);
                bw.BaseStream.Seek(0x07, SeekOrigin.Current);
                bw.Write(entry.MaxComboHits);
                bw.Write(thirdFlags);
                bw.Write(entry.AngleDetection);
                bw.Write(entry.RadiusDetection);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.SoundAndMagickDetection);
                bw.Write(entry.LifeDetection);
                bw.Write(entry.Unknown1);
                bw.Write(fourthFlags);
                bw.Write((byte)entry.ElementalAffinitiesAbsorb);
                bw.Write((byte)entry.ElementalAffinitiesHalfDamage);
                bw.Write((byte)entry.ElementalAffinitiesImmune);
                bw.Write((byte)entry.ElementalAffinitiesWeak);
                bw.Write((byte)entry.ElementalAffinitiesPotency);
                bw.BaseStream.Seek(0x12, SeekOrigin.Current);
                bw.Write(entry.ChainIdentifier);
                bw.BaseStream.Seek(0x10, SeekOrigin.Current);
                bw.Write(entry.BestiaryIdentifier);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Model")]
            public int Model { get; set; }

            [JsonPropertyName("Classification")]
            public byte Classification { get; set; }

            [JsonPropertyName("Genus")]
            public byte Genus { get; set; }

            [JsonPropertyName("Weight")]
            public short Weight { get; set; }

            [JsonPropertyName("Max Combo Hits")]
            public byte MaxComboHits { get; set; }

            [JsonPropertyName("Angle Detection")]
            public byte AngleDetection { get; set; }

            [JsonPropertyName("Radius Detection")]
            public byte RadiusDetection { get; set; }

            [JsonPropertyName("Sound And Magick Detection")]
            public byte SoundAndMagickDetection { get; set; }

            [JsonPropertyName("Life Detection")]
            public byte LifeDetection { get; set; }

            [JsonPropertyName("Chain Identifier")]
            public ushort ChainIdentifier { get; set; }

            [JsonPropertyName("Bestiary Identifier")]
            public ushort BestiaryIdentifier { get; set; }

            [JsonPropertyName("Elemental Affinities (Absorb)")]
            public ElementsEnum ElementalAffinitiesAbsorb { get; set; }

            [JsonPropertyName("Elemental Affinities (Immune)")]
            public ElementsEnum ElementalAffinitiesImmune { get; set; }

            [JsonPropertyName("Elemental Affinities (Half Damage)")]
            public ElementsEnum ElementalAffinitiesHalfDamage { get; set; }

            [JsonPropertyName("Elemental Affinities (Weak)")]
            public ElementsEnum ElementalAffinitiesWeak { get; set; }

            [JsonPropertyName("Elemental Affinities (Potency)")]
            public ElementsEnum ElementalAffinitiesPotency { get; set; }

            private byte chargeAuraAnimationScale;
            [JsonPropertyName("Charge Aura Animation Scale")]
            public byte ChargeAuraAnimationScale
            {
                get => chargeAuraAnimationScale;
                set
                {
                    if (value > 4)
                    {
                        throw new ArgumentException("Ard Section 2: 'Charge Aura Animation Scale' cannot be higher than 4.");
                    }
                    chargeAuraAnimationScale = value;
                }
            }

            [JsonPropertyName("Has Collision")]
            public bool HasCollision { get; set; }

            [JsonPropertyName("Has Flying Info")]
            public bool HasFlyingInfo { get; set; }

            [JsonPropertyName("Is Flying")]
            public bool IsFlying { get; set; }

            [JsonPropertyName("Is Floating")]
            public bool IsFloating { get; set; }

            [JsonPropertyName("Use Teleport Attack")]
            public bool UseTeleportAttack { get; set; }

            [JsonPropertyName("Use Distanced Attack")]
            public bool UseDistancedAttack { get; set; }

            [JsonPropertyName("Ignore Chain")]
            public bool IgnoreChain { get; set; }

            [JsonPropertyName("Unknown 0")]
            public ushort Unknown0 { get; set; }

            [JsonPropertyName("Unknown 1")]
            public byte Unknown1 { get; set; }

            [JsonPropertyName("Unknown Flag 0")]
            public bool UnknownFlag0 { get; set; }

            [JsonPropertyName("Unknown Flag 1")]
            public bool UnknownFlag1 { get; set; }

            private byte unknownFlag2;
            [JsonPropertyName("Unknown Flag 2")]
            public byte UnknownFlag2
            {
                get => unknownFlag2;
                set
                {
                    if (value > 7)
                    {
                        throw new ArgumentException("Ard Section 2: 'Unknown Flag 2' cannot be higher than 7.");
                    }
                    unknownFlag2 = value;
                }
            }

            [JsonPropertyName("Unknown Flag 3")]
            public bool UnknownFlag3 { get; set; }
        }
    }
}
