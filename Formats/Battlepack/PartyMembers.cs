using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class PartyMembers : St2e
    {
        [JsonPropertyName("Party Members")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public PartyMembers(Dictionary<string, Entry> entries)
        {
            if (entries.Values.Any(i => i.Quickenings.Length > 3))
            {
                throw new ArgumentException("Battlepack Section 16: 'Quickenings' must contain exactly 3 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0x80);
        }

        public PartyMembers(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.RestrictedEquipmentCategories = (EquipmentCategories)br.ReadUInt32();

                for (var j = 0; j < 3; j++)
                {
                    entry.Quickenings[j] = br.ReadByte();
                }

                entry.CameraPositionLink = br.ReadByte();
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                entry.Weapon = br.ReadUInt16();
                entry.Offhand = br.ReadUInt16();
                entry.Helm = br.ReadUInt16();
                entry.Armor = br.ReadUInt16();
                entry.Accessory = br.ReadUInt16();
                entry.DefaultPartyMemberGambitsLink = (ushort)(br.ReadUInt16() - 0x5000);
                entry.MaxHp = br.ReadUInt16();
                entry.MaxHpExtendedModifier = br.ReadByte();
                entry.MaxHpBaseModifier = br.ReadByte();
                entry.MaxMp = br.ReadUInt16();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.MaxMpModifier = br.ReadByte();
                entry.Strength = br.ReadByte();
                entry.StrengthModifier = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.MagickPower = br.ReadByte();
                entry.MagickPowerModifier = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.Vitality = br.ReadByte();
                entry.VitalityModifier = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.Speed = br.ReadByte();
                entry.SpeedModifier = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.Evade = br.ReadByte();
                entry.DetectionRange = br.ReadByte();
                var flags = br.ReadByte();
                entry.RecalculateStatsOnPartyJoin = (byte)(flags >> 3 & 0x03);
                entry.GambitState = (flags >> 6 & 0x01) == 1;
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.Level = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.Name = br.ReadUInt16();
                entry.SummonTime = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);

                var quantities = new byte[10];
                for (var j = 0; j < quantities.Length; j++)
                {
                    quantities[j] = br.ReadByte();
                }

                entry.Gil = br.ReadUInt16();
                br.BaseStream.Seek(0x04, SeekOrigin.Current);
                entry.Lp = br.ReadUInt16();
                entry.MistBars = br.ReadByte();
                entry.InitialMpPercentage = br.ReadByte();
                entry.StatusEffects = (StatusEffectsEnum)br.ReadUInt32();
                entry.StatusEffectImmunities = (StatusEffectsEnum)br.ReadUInt32();
                entry.Augments = (AugmentsEnum)br.ReadUInt64();

                var contents = new ushort[10];
                for (var j = 0; j < contents.Length; j++)
                {
                    contents[j] = br.ReadUInt16();
                }

                for (var j = 0; j < contents.Length; j++)
                {
                    var inventory = new Inventory
                    {
                        Content = contents[j],
                        Quantity = quantities[j]
                    };
                    entry.InventoryDic.Add($"Inventory {j}", inventory);
                }

                br.BaseStream.Seek(0x04, SeekOrigin.Current);
                entry.Model = br.ReadInt32();
                br.BaseStream.Seek(0x06, SeekOrigin.Current);
                entry.Weight = br.ReadInt16();
                br.BaseStream.Seek(0x04, SeekOrigin.Current);
                Entries.Add($"Party Member {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                byte flags = 0;
                flags |= (byte)(entry.RecalculateStatsOnPartyJoin << 3);
                flags |= entry.GambitState ? (byte)0x40 : (byte)0;

                bw.Write((uint)entry.RestrictedEquipmentCategories);
                bw.Write(entry.Quickenings);
                bw.Write(entry.CameraPositionLink);
                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                bw.Write(entry.Weapon);
                bw.Write(entry.Offhand);
                bw.Write(entry.Helm);
                bw.Write(entry.Armor);
                bw.Write(entry.Accessory);
                bw.Write((ushort)(entry.DefaultPartyMemberGambitsLink + 0x5000));
                bw.Write(entry.MaxHp);
                bw.Write(entry.MaxHpExtendedModifier);
                bw.Write(entry.MaxHpBaseModifier);
                bw.Write(entry.MaxMp);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.MaxMpModifier);
                bw.Write(entry.Strength);
                bw.Write(entry.StrengthModifier);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.MagickPower);
                bw.Write(entry.MagickPowerModifier);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.Vitality);
                bw.Write(entry.VitalityModifier);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.Speed);
                bw.Write(entry.SpeedModifier);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.Evade);
                bw.Write(entry.DetectionRange);
                bw.Write(flags);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.Level);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.Name);
                bw.Write(entry.SummonTime);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.InventoryDic.Values.Select(i => i.Quantity).ToArray());
                bw.Write(entry.Gil);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                bw.Write(entry.Lp);
                bw.Write(entry.MistBars);
                bw.Write(entry.InitialMpPercentage);
                bw.Write((uint)entry.StatusEffects);
                bw.Write((uint)entry.StatusEffectImmunities);
                bw.Write((ulong)entry.Augments);

                foreach (var inventory in entry.InventoryDic.Values)
                {
                    bw.Write(inventory.Content);
                }

                bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                bw.Write(entry.Model);
                bw.BaseStream.Seek(0x06, SeekOrigin.Current);
                bw.Write(entry.Weight);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Name")]
            public ushort Name { get; set; }

            [JsonPropertyName("Model")]
            public int Model { get; set; }

            [JsonPropertyName("Restricted Equipment Categories")]
            public EquipmentCategories RestrictedEquipmentCategories { get; set; }

            [JsonPropertyName("Weapon")]
            public ushort Weapon { get; set; }

            [JsonPropertyName("Off-hand")]
            public ushort Offhand { get; set; }

            [JsonPropertyName("Helm")]
            public ushort Helm { get; set; }

            [JsonPropertyName("Armor")]
            public ushort Armor { get; set; }

            [JsonPropertyName("Accessory")]
            public ushort Accessory { get; set; }

            [JsonPropertyName("Inventory / Guest And Esper Abilities List")]
            public Dictionary<string, Inventory> InventoryDic { get; set; }

            [JsonPropertyName("Status Effect Immunities")]
            public StatusEffectsEnum StatusEffectImmunities { get; set; }

            [JsonPropertyName("Status Effects")]
            public StatusEffectsEnum StatusEffects { get; set; }

            [JsonPropertyName("Augments")]
            public AugmentsEnum Augments { get; set; }

            [JsonPropertyName("Max HP")]
            public ushort MaxHp { get; set; }

            [JsonPropertyName("Max HP Base Modifier")]
            public byte MaxHpBaseModifier { get; set; }

            [JsonPropertyName("Max HP Extended Modifier")]
            public byte MaxHpExtendedModifier { get; set; }

            [JsonPropertyName("Max MP")]
            public ushort MaxMp { get; set; }

            [JsonPropertyName("Max MP Modifier")]
            public byte MaxMpModifier { get; set; }

            [JsonPropertyName("Initial MP Percentage")]
            public byte InitialMpPercentage { get; set; }

            [JsonPropertyName("Strength")]
            public byte Strength { get; set; }

            [JsonPropertyName("Strength Modifier")]
            public byte StrengthModifier { get; set; }

            [JsonPropertyName("Magick Power")]
            public byte MagickPower { get; set; }

            [JsonPropertyName("Magick Power Modifier")]
            public byte MagickPowerModifier { get; set; }

            [JsonPropertyName("Vitality")]
            public byte Vitality { get; set; }

            [JsonPropertyName("Vitality Modifier")]
            public byte VitalityModifier { get; set; }

            [JsonPropertyName("Speed")]
            public byte Speed { get; set; }

            [JsonPropertyName("Speed Modifier")]
            public byte SpeedModifier { get; set; }

            [JsonPropertyName("Evade")]
            public byte Evade { get; set; }

            [JsonPropertyName("LP")]
            public ushort Lp { get; set; }

            [JsonPropertyName("Level")]
            public byte Level { get; set; }

            [JsonPropertyName("Gil")]
            public ushort Gil { get; set; }

            [JsonPropertyName("Mist Bars")]
            public byte MistBars { get; set; }

            [JsonPropertyName("Quickenings")]
            public byte[] Quickenings { get; set; }

            [JsonPropertyName("Summon Time")]
            public byte SummonTime { get; set; }

            [JsonPropertyName("Detection Range")]
            public byte DetectionRange { get; set; }

            [JsonPropertyName("Weight")]
            public short Weight { get; set; }

            [JsonPropertyName("Default Party Member Gambits Link")]
            public ushort DefaultPartyMemberGambitsLink { get; set; }

            [JsonPropertyName("Camera Position Link")]
            public byte CameraPositionLink { get; set; }

            private byte recalculateStatsOnPartyJoin;

            [JsonPropertyName("Recalculate Stats On Party Join")]
            public byte RecalculateStatsOnPartyJoin
            {
                get => recalculateStatsOnPartyJoin;
                set
                {
                    if (value > 3)
                    {
                        throw new ArgumentException("Battlepack Section 16: 'Recalculate Stats On Party Join' cannot be higher than 3.");
                    }
                    recalculateStatsOnPartyJoin = value;
                }

            }

            [JsonPropertyName("Gambit State")]
            public bool GambitState { get; set; }

            public Entry()
            {
                Quickenings = new byte[3];
                InventoryDic = new Dictionary<string, Inventory>();
            }
        }

        public class Inventory
        {
            [JsonPropertyName("Content")]
            public ushort Content { get; set; }

            [JsonPropertyName("Quantity")]
            public byte Quantity { get; set; }
        }

        [Flags]
        public enum EquipmentCategories : uint
        {
            None = 0x00000000,
            Unarmed = 0x00000001,
            Sword = 0x00000002,
            Greatsword = 0x00000004,
            Katana = 0x00000008,
            NinjaSword = 0x00000010,
            Spear = 0x00000020,
            Pole = 0x00000040,
            Bow = 0x00000080,
            Crossbow = 0x00000100,
            Gun = 0x00000200,
            Axe = 0x00000400,
            Hammer = 0x00000800,
            Dagger = 0x00001000,
            Rod = 0x00002000,
            Staff = 0x00004000,
            Mace = 0x00008000,
            Measure = 0x00010000,
            HandBomb = 0x00020000,
            Shield = 0x00040000,
            Helm = 0x00080000,
            Armor = 0x00100000,
            Accessory = 0x00200000,
            AccessoryCrown = 0x00400000,
            Arrow = 0x00800000,
            Bolt = 0x01000000,
            Shot = 0x02000000,
            Bomb = 0x04000000
        }
    }
}