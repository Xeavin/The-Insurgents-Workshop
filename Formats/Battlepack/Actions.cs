using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Actions : St2e
    {
        [JsonPropertyName("Actions")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Actions(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x3C);
        }

        public Actions(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.BattleMenuDescription = br.ReadUInt16();
                entry.DashRangeTo = br.ReadByte();
                entry.DashRangeFrom = br.ReadByte();
                entry.KnockbackChance = br.ReadByte();
                entry.Range = br.ReadByte();
                entry.AreaOfEffectRange = br.ReadByte();
                entry.AreaOfEffectPositioningRange = br.ReadByte();
                entry.Formula = br.ReadByte();
                entry.ChargeTime = br.ReadByte();
                entry.MpOrMistCost = br.ReadByte();
                entry.Unknown0 = br.ReadByte();
                entry.FirstFlags = br.ReadUInt32();
                entry.Power = br.ReadByte();
                entry.PowerMultiplier = br.ReadByte();
                entry.AccuracyRate = br.ReadByte();
                entry.Elements = (ElementsEnum)br.ReadByte();
                entry.OnHitRate = br.ReadByte();
                entry.AdditionalPowerMultiplier = br.ReadByte();
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                entry.StatusEffects = (StatusEffectsEnum)br.ReadUInt32();
                entry.CastAnimation = br.ReadByte();
                entry.AddEnmityToSelfByFoe = br.ReadByte();
                entry.BattleMenuCategory = br.ReadByte();
                entry.AddEnmityToSelfByParty = br.ReadByte();
                entry.RemoveEnmityOffOtherAlliesByFoe = br.ReadByte();
                entry.ChargeAuraAnimation = br.ReadByte();
                entry.RequiredContent = br.ReadUInt16();
                entry.AfterAnimation = br.ReadUInt16();
                entry.SummonedPartyMember = br.ReadUInt16();
                entry.MistAction = br.ReadUInt16();
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                entry.SecondFlags = br.ReadUInt16();
                entry.MenuDescription = br.ReadUInt16();
                entry.CharacterAnimationLink = br.ReadUInt32();
                entry.Name = br.ReadUInt16();
                entry.AiTargetConditionFlags = br.ReadUInt16();
                entry.GambitPage = br.ReadSByte();
                entry.GambitPageOrder = br.ReadSByte();
                entry.EnableAiFlag = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                Entries.Add($"Action {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.BattleMenuDescription);
                bw.Write(entry.DashRangeTo);
                bw.Write(entry.DashRangeFrom);
                bw.Write(entry.KnockbackChance);
                bw.Write(entry.Range);
                bw.Write(entry.AreaOfEffectRange);
                bw.Write(entry.AreaOfEffectPositioningRange);
                bw.Write(entry.Formula);
                bw.Write(entry.ChargeTime);
                bw.Write(entry.MpOrMistCost);
                bw.Write(entry.Unknown0);
                bw.Write(entry.FirstFlags);
                bw.Write(entry.Power);
                bw.Write(entry.PowerMultiplier);
                bw.Write(entry.AccuracyRate);
                bw.Write((byte)entry.Elements);
                bw.Write(entry.OnHitRate);
                bw.Write(entry.AdditionalPowerMultiplier);
                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                bw.Write((uint)entry.StatusEffects);
                bw.Write(entry.CastAnimation);
                bw.Write(entry.AddEnmityToSelfByFoe);
                bw.Write(entry.BattleMenuCategory);
                bw.Write(entry.AddEnmityToSelfByParty);
                bw.Write(entry.RemoveEnmityOffOtherAlliesByFoe);
                bw.Write(entry.ChargeAuraAnimation);
                bw.Write(entry.RequiredContent);
                bw.Write(entry.AfterAnimation);
                bw.Write(entry.SummonedPartyMember);
                bw.Write(entry.MistAction);
                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                bw.Write(entry.SecondFlags);
                bw.Write(entry.MenuDescription);
                bw.Write(entry.CharacterAnimationLink);
                bw.Write(entry.Name);
                bw.Write(entry.AiTargetConditionFlags);
                bw.Write(entry.GambitPage);
                bw.Write(entry.GambitPageOrder);
                bw.Write(entry.EnableAiFlag);
                bw.Write(new byte());
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Name")]
            public ushort Name { get; set; }

            [JsonPropertyName("Menu Description")]
            public ushort MenuDescription { get; set; }

            [JsonPropertyName("Battle Menu Description")]
            public ushort BattleMenuDescription { get; set; }

            [JsonPropertyName("Battle Menu Category")]
            public byte BattleMenuCategory { get; set; }

            [JsonPropertyName("Required Content")]
            public ushort RequiredContent { get; set; }

            [JsonPropertyName("Charge Time")]
            public byte ChargeTime { get; set; }

            [JsonPropertyName("MP/Mist Cost")]
            public byte MpOrMistCost { get; set; }

            [JsonPropertyName("Formula")]
            public byte Formula { get; set; }

            [JsonPropertyName("Power")]
            public byte Power { get; set; }

            [JsonPropertyName("Power Multiplier")]
            public byte PowerMultiplier { get; set; }

            [JsonPropertyName("Additional Power Multiplier")]
            public byte AdditionalPowerMultiplier { get; set; }

            [JsonPropertyName("Knockback Chance")]
            public byte KnockbackChance { get; set; }

            [JsonPropertyName("Accuracy Rate")]
            public byte AccuracyRate { get; set; }
            [JsonPropertyName("On-Hit Rate")]
            public byte OnHitRate { get; set; }

            [JsonPropertyName("Status Effects")]
            public StatusEffectsEnum StatusEffects { get; set; }

            [JsonPropertyName("Elements")]
            public ElementsEnum Elements { get; set; }

            [JsonPropertyName("Range")]
            public byte Range { get; set; }

            [JsonPropertyName("Area Of Effect Range")]
            public byte AreaOfEffectRange { get; set; }

            [JsonPropertyName("Area Of Effect Positioning Range")]
            public byte AreaOfEffectPositioningRange { get; set; }

            [JsonPropertyName("Add Enmity To Self (Target: Foe)")]
            public byte AddEnmityToSelfByFoe { get; set; }

            [JsonPropertyName("Add Enmity To Self (Target: Ally)")]
            public byte AddEnmityToSelfByParty { get; set; }

            [JsonPropertyName("Remove Enmity From Other Allies (Target: Foe)")]
            public byte RemoveEnmityOffOtherAlliesByFoe { get; set; }

            [JsonPropertyName("Character Animation Link")]
            public uint CharacterAnimationLink { get; set; }

            [JsonPropertyName("Charge Aura Animation")]
            public byte ChargeAuraAnimation { get; set; }

            [JsonPropertyName("Cast Animation")]
            public byte CastAnimation { get; set; }

            [JsonPropertyName("After Animation")]
            public ushort AfterAnimation { get; set; }

            [JsonPropertyName("Dash Range To")]
            public byte DashRangeTo { get; set; }

            [JsonPropertyName("Dash Range From")]
            public byte DashRangeFrom { get; set; }

            private sbyte gambitPage;

            [JsonPropertyName("Gambit Page")]
            public sbyte GambitPage
            {
                get => gambitPage;
                set
                {
                    if (value > 10)
                    {
                        throw new ArgumentException("Battlepack Section 14: 'Gambit Page' must be -1 or lower than 11.");
                    }
                    gambitPage = value;
                }
            }

            private sbyte gambitPageOrder;

            [JsonPropertyName("Gambit Page Order")]
            public sbyte GambitPageOrder
            {
                get => gambitPageOrder;
                set
                {
                    if (value > 16)
                    {
                        throw new ArgumentException("Battlepack Section 14: 'Gambit Page Order' must be -1 or lower than 17.");
                    }
                    gambitPageOrder = value;
                }
            }

            [JsonPropertyName("First Flags")]
            public uint FirstFlags { get; set; }

            [JsonPropertyName("Second Flags")]
            public ushort SecondFlags { get; set; }

            [JsonPropertyName("AI Target Condition Flags")]
            public ushort AiTargetConditionFlags { get; set; }

            [JsonPropertyName("Enable AI Flag")]
            public byte EnableAiFlag { get; set; }

            [JsonPropertyName("Summoned Party Member")]
            public ushort SummonedPartyMember { get; set; }

            [JsonPropertyName("Mist Action")]
            public ushort MistAction { get; set; }

            [JsonPropertyName("Esper Technick Rank?")]
            public byte Unknown0 { get; set; }
        }
    }
}