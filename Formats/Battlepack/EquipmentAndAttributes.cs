using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class EquipmentAndAttributes : St2e
    {
        [JsonPropertyName("Equipment List")]
        [JsonConverter(typeof(EquipmentConverter))]
        public Dictionary<string, dynamic> EquipmentDic { get; set; }

        [JsonPropertyName("Attributes")]
        public Dictionary<string, Attribute> Attributes { get; set; }

        [JsonConstructor]
        public EquipmentAndAttributes(Dictionary<string, dynamic> equipmentDic, Dictionary<string, Attribute> attributes)
        {
            EquipmentDic = equipmentDic;
            Attributes = attributes;
            var attributeListOffset = equipmentDic.Count * 0x34 + 0x20; //equipmentCount * equipmentSize + headerSize
            SetupHeader((uint)equipmentDic.Count, 0x34, 0, 0, (uint)attributeListOffset);
        }

        public EquipmentAndAttributes(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            EquipmentDic = new Dictionary<string, dynamic>();
            for (var i = 0; i < EntryCount; i++)
            {
                var reservedPos = br.BaseStream.Position;
                br.BaseStream.Seek(0x09, SeekOrigin.Current);
                var category = br.ReadByte();
                br.BaseStream.Position = reservedPos;

                dynamic data = category switch
                {
                    >= 0 and <= 17 => new Weapon(),
                    18 => new Shield(),
                    >= 19 and <= 22 => new HelmArmorAccessory(),
                    >= 23 => new Ammunition()
                };

                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                data.Name = br.ReadUInt16();
                data.Icon = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                data.OptimizationTiebreaker = br.ReadByte();
                var flags = br.ReadByte();
                data.Unsaleable = (flags >> 1 & 0x01) == 1;
                data.HitsFlying = (flags >> 2 & 0x01) == 1;
                data.LicenseIndependent = (flags >> 4 & 0x01) == 1;
                data.SlotType = (byte)(flags >> 5 & 0x07);
                data.Sort = br.ReadByte();
                data.Category = br.ReadByte();
                br.BaseStream.Seek(0x04, SeekOrigin.Current);
                data.Description = br.ReadUInt16();
                data.Metal = br.ReadByte();

                switch ((int)data.Category)
                {
                    case >= 0 and <= 17: //weapon
                        data.OffhandCategory = br.ReadByte();
                        data.Gil = br.ReadUInt16();
                        br.BaseStream.Seek(0x04, SeekOrigin.Current);
                        data.Range = br.ReadByte();
                        data.Formula = br.ReadByte();
                        data.AttackPower = br.ReadByte();
                        data.KnockbackChance = br.ReadByte();
                        data.ComboOrCriticalChance = br.ReadByte();
                        data.Evade = br.ReadByte();
                        data.Elements = (ElementsEnum)br.ReadByte();
                        data.OnHitRate = br.ReadByte();
                        data.StatusEffects = (StatusEffectsEnum)br.ReadUInt32();
                        br.BaseStream.Seek(0x01, SeekOrigin.Current);
                        data.DistanceBehaviour = br.ReadByte();
                        data.Stance = br.ReadByte();
                        data.ChargeTime = br.ReadByte();
                        data.AttributeLink = br.ReadUInt32() / 0x18; //attributeOffset / attributeSize
                        data.Particles = br.ReadUInt16();
                        br.BaseStream.Seek(0x02, SeekOrigin.Current);
                        data.Model = br.ReadInt32();
                        break;
                    case 18: //shield
                        br.BaseStream.Seek(0x01, SeekOrigin.Current);
                        data.Gil = br.ReadUInt16();
                        br.BaseStream.Seek(0x04, SeekOrigin.Current);
                        data.Evade = br.ReadByte();
                        data.MagickEvade = br.ReadByte();
                        br.BaseStream.Seek(0x0E, SeekOrigin.Current);
                        data.AttributeLink = br.ReadUInt32() / 0x18; //attributeOffset / attributeSize
                        br.BaseStream.Seek(0x04, SeekOrigin.Current);
                        data.Model = br.ReadInt32();
                        break;
                    case >= 19 and <= 22: //helm, armor or accessory
                        br.BaseStream.Seek(0x01, SeekOrigin.Current);
                        data.Gil = br.ReadUInt16();
                        br.BaseStream.Seek(0x04, SeekOrigin.Current);
                        data.Defense = br.ReadByte();
                        data.MagickResist = br.ReadByte();
                        data.Augment = br.ReadByte();
                        br.BaseStream.Seek(0x0D, SeekOrigin.Current);
                        data.AttributeLink = br.ReadUInt32() / 0x18; //attributeOffset / attributeSize
                        br.BaseStream.Seek(0x08, SeekOrigin.Current);
                        break;
                    case >= 23: //ammunition
                        br.BaseStream.Seek(0x01, SeekOrigin.Current);
                        data.Gil = br.ReadUInt16();
                        br.BaseStream.Seek(0x06, SeekOrigin.Current);
                        data.AttackPower = br.ReadByte();
                        br.BaseStream.Seek(0x02, SeekOrigin.Current);
                        data.Evade = br.ReadByte();
                        data.Elements = (ElementsEnum)br.ReadByte();
                        data.OnHitRate = br.ReadByte();
                        data.StatusEffects = (StatusEffectsEnum)br.ReadUInt32();
                        br.BaseStream.Seek(0x04, SeekOrigin.Current);
                        data.AttributeLink = br.ReadUInt32() / 0x18; //attributeOffset / attributeSize
                        br.BaseStream.Seek(0x04, SeekOrigin.Current);
                        data.Model = br.ReadInt32();
                        break;
                    default:
                        throw new ArgumentException("Battlepack Section 13: 'Equipment -> Type' is unknown.");
                }
                EquipmentDic.Add($"Equipment {i}", data);
            }

            br.BaseStream.Seek(UnknownOffset1, SeekOrigin.Begin);
            var attributeCount = (ushort)((br.BaseStream.Length - UnknownOffset1) / 0x18); //(end of file offset - attribute list offset) / attribute data size

            Attributes = new Dictionary<string, Attribute>();
            for (var i = 0; i < attributeCount; i++)
            {
                var data = new Attribute
                {
                    MaxHp = br.ReadUInt16(),
                    MaxMp = br.ReadUInt16(),
                    Strength = br.ReadByte(),
                    MagickPower = br.ReadByte(),
                    Vitality = br.ReadByte(),
                    Speed = br.ReadByte(),
                    StatusEffects = (StatusEffectsEnum)br.ReadUInt32(),
                    StatusEffectImmunities = (StatusEffectsEnum)br.ReadUInt32(),
                    ElementalAffinitiesAbsorb = (ElementsEnum)br.ReadByte(),
                    ElementalAffinitiesImmune = (ElementsEnum)br.ReadByte(),
                    ElementalAffinitiesHalfDamage = (ElementsEnum)br.ReadByte(),
                    ElementalAffinitiesWeak = (ElementsEnum)br.ReadByte(),
                    ElementalAffinitiesPotency = (ElementsEnum)br.ReadByte()
                };
                br.BaseStream.Seek(0x03, SeekOrigin.Current);
                Attributes.Add($"Attribute {i}", data);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var equip in EquipmentDic.Values)
            {
                byte flags = 0;
                flags |= equip.Unsaleable ? (byte)0x02 : (byte)0;
                flags |= equip.HitsFlying ? (byte)0x04 : (byte)0;
                flags |= equip.LicenseIndependent ? (byte)0x10 : (byte)0;
                flags |= (byte)(equip.SlotType << 5);

                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                bw.Write(equip.Name);
                bw.Write(equip.Icon);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(equip.OptimizationTiebreaker);
                bw.Write(flags);
                bw.Write(equip.Sort);
                bw.Write(equip.Category);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                bw.Write(equip.Description);
                bw.Write(equip.Metal);

                switch (equip.Type)
                {
                    case Equipment.Types.Weapon:
                        bw.Write(equip.OffhandCategory);
                        bw.Write(equip.Gil);
                        bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                        bw.Write(equip.Range);
                        bw.Write(equip.Formula);
                        bw.Write(equip.AttackPower);
                        bw.Write(equip.KnockbackChance);
                        bw.Write(equip.ComboOrCriticalChance);
                        bw.Write(equip.Evade);
                        bw.Write((byte)equip.Elements);
                        bw.Write(equip.OnHitRate);
                        bw.Write((uint)equip.StatusEffects);
                        bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                        bw.Write(equip.DistanceBehaviour);
                        bw.Write(equip.Stance);
                        bw.Write(equip.ChargeTime);
                        bw.Write(equip.AttributeLink * 0x18); //attributeLink * attributeSize
                        bw.Write(equip.Particles);
                        bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                        bw.Write(equip.Model);
                        break;
                    case Equipment.Types.Shield:
                        bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                        bw.Write(equip.Gil);
                        bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                        bw.Write(equip.Evade);
                        bw.Write(equip.MagickEvade);
                        bw.BaseStream.Seek(0x0E, SeekOrigin.Current);
                        bw.Write(equip.AttributeLink * 0x18); //attributeLink * attributeSize
                        bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                        bw.Write(equip.Model);
                        break;
                    case Equipment.Types.HelmArmorAccessory:
                        bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                        bw.Write(equip.Gil);
                        bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                        bw.Write(equip.Defense);
                        bw.Write(equip.MagickResist);
                        bw.Write(equip.Augment);
                        bw.BaseStream.Seek(0x0D, SeekOrigin.Current);
                        bw.Write(equip.AttributeLink * 0x18); //attributeLink * attributeSize
                        bw.BaseStream.Seek(0x08, SeekOrigin.Current);
                        break;
                    case Equipment.Types.Ammunition:
                        bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                        bw.Write(equip.Gil);
                        bw.BaseStream.Seek(0x06, SeekOrigin.Current);
                        bw.Write(equip.AttackPower);
                        bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                        bw.Write(equip.Evade);
                        bw.Write((byte)equip.Elements);
                        bw.Write(equip.OnHitRate);
                        bw.Write((uint)equip.StatusEffects);
                        bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                        bw.Write(equip.AttributeLink * 0x18); //attributeLink * attributeSize
                        bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                        bw.Write(equip.Model);
                        break;
                    default:
                        throw new ArgumentException("Battlepack Section 13: 'Equipment -> Type' is unknown.");
                }
            }

            bw.BaseStream.Seek(UnknownOffset1, SeekOrigin.Begin);
            foreach (var atr in Attributes.Values)
            {
                bw.Write(atr.MaxHp);
                bw.Write(atr.MaxMp);
                bw.Write(atr.Strength);
                bw.Write(atr.MagickPower);
                bw.Write(atr.Vitality);
                bw.Write(atr.Speed);
                bw.Write((uint)atr.StatusEffects);
                bw.Write((uint)atr.StatusEffectImmunities);
                bw.Write((byte)atr.ElementalAffinitiesAbsorb);
                bw.Write((byte)atr.ElementalAffinitiesImmune);
                bw.Write((byte)atr.ElementalAffinitiesHalfDamage);
                bw.Write((byte)atr.ElementalAffinitiesWeak);
                bw.Write((byte)atr.ElementalAffinitiesPotency);
                bw.Write(new byte[3]);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Equipment
        {
            [JsonPropertyName("Type")]
            [JsonPropertyOrder(-14)]
            public Types Type { get; set; }

            [JsonPropertyName("Name")]
            [JsonPropertyOrder(-13)]
            public ushort Name { get; set; }

            [JsonPropertyName("Description (Ineffectual)")]
            [JsonPropertyOrder(-12)]
            public ushort Description { get; set; }

            [JsonPropertyName("Category")]
            [JsonPropertyOrder(-11)]
            public byte Category { get; set; }

            private byte slotType;
            [JsonPropertyName("Slot Type")]
            [JsonPropertyOrder(-10)]
            public byte SlotType
            {
                get => slotType;
                set
                {
                    if (value > 4)
                    {
                        throw new ArgumentException("Battlepack Section 13: 'Equipment - Slot Type' cannot be higher than 4.");
                    }
                    slotType = value;
                }
            }

            [JsonPropertyName("Icon")]
            [JsonPropertyOrder(-9)]
            public byte Icon { get; set; }

            [JsonPropertyName("Metal")]
            [JsonPropertyOrder(-8)]
            public byte Metal { get; set; }

            [JsonPropertyName("Gil")]
            [JsonPropertyOrder(-7)]
            public ushort Gil { get; set; }

            [JsonPropertyName("Attribute Link")]
            [JsonPropertyOrder(-6)]
            public uint AttributeLink { get; set; }

            [JsonPropertyName("Sort")]
            [JsonPropertyOrder(-5)]
            public byte Sort { get; set; }

            [JsonPropertyName("Optimization Tiebreaker")]
            [JsonPropertyOrder(-4)]
            public byte OptimizationTiebreaker { get; set; }

            [JsonPropertyName("Hits Flying")]
            [JsonPropertyOrder(-3)]
            public bool HitsFlying { get; set; }

            [JsonPropertyName("License Independent")]
            [JsonPropertyOrder(-2)]
            public bool LicenseIndependent { get; set; }

            [JsonPropertyName("Unsaleable")]
            [JsonPropertyOrder(-1)]
            public bool Unsaleable { get; set; }

            public enum Types : byte
            {
                Weapon = 0,
                Shield = 1,
                Ammunition = 2,
                HelmArmorAccessory = 3
            }
        }

        public class Weapon : Equipment
        {
            [JsonPropertyName("Off-hand Category")]
            [JsonPropertyOrder(0)]
            public byte OffhandCategory { get; set; }

            [JsonPropertyName("Formula")]
            [JsonPropertyOrder(1)]
            public byte Formula { get; set; }

            [JsonPropertyName("Range")]
            [JsonPropertyOrder(2)]
            public byte Range { get; set; }

            [JsonPropertyName("Charge Time")]
            [JsonPropertyOrder(3)]
            public byte ChargeTime { get; set; }

            [JsonPropertyName("Attack Power")]
            [JsonPropertyOrder(4)]
            public byte AttackPower { get; set; }

            [JsonPropertyName("Evade")]
            [JsonPropertyOrder(5)]
            public byte Evade { get; set; }

            [JsonPropertyName("Knockback Chance")]
            [JsonPropertyOrder(6)]
            public byte KnockbackChance { get; set; }

            [JsonPropertyName("Combo/Critical Chance")]
            [JsonPropertyOrder(7)]
            public byte ComboOrCriticalChance { get; set; }

            [JsonPropertyName("On-Hit Rate")]
            [JsonPropertyOrder(8)]
            public byte OnHitRate { get; set; }

            [JsonPropertyName("Status Effects")]
            [JsonPropertyOrder(9)]
            public StatusEffectsEnum StatusEffects { get; set; }

            [JsonPropertyName("Elements")]
            [JsonPropertyOrder(10)]
            public ElementsEnum Elements { get; set; }

            [JsonPropertyName("Model")]
            [JsonPropertyOrder(11)]
            public int Model { get; set; }

            [JsonPropertyName("Particles")]
            [JsonPropertyOrder(12)]
            public ushort Particles { get; set; }

            [JsonPropertyName("Stance")]
            [JsonPropertyOrder(13)]
            public byte Stance { get; set; }

            [JsonPropertyName("Distance Behaviour")]
            [JsonPropertyOrder(14)]
            public byte DistanceBehaviour { get; set; }

            public Weapon()
            {
                Type = Types.Weapon;
            }
        }

        public class Ammunition : Equipment
        {
            [JsonPropertyName("Attack Power")]
            [JsonPropertyOrder(0)]
            public byte AttackPower { get; set; }

            [JsonPropertyName("Evade")]
            [JsonPropertyOrder(1)]
            public byte Evade { get; set; }

            [JsonPropertyName("On-Hit Rate")]
            [JsonPropertyOrder(2)]
            public byte OnHitRate { get; set; }

            [JsonPropertyName("Status Effects")]
            [JsonPropertyOrder(3)]
            public StatusEffectsEnum StatusEffects { get; set; }

            [JsonPropertyName("Elements")]
            [JsonPropertyOrder(4)]
            public ElementsEnum Elements { get; set; }

            [JsonPropertyName("Model")]
            [JsonPropertyOrder(5)]
            public int Model { get; set; }

            public Ammunition()
            {
                Type = Types.Ammunition;
            }
        }

        public class Shield : Equipment
        {
            [JsonPropertyName("Evade")]
            [JsonPropertyOrder(0)]
            public byte Evade { get; set; }

            [JsonPropertyName("Magick Evade")]
            [JsonPropertyOrder(1)]
            public byte MagickEvade { get; set; }

            [JsonPropertyName("Model")]
            [JsonPropertyOrder(2)]
            public int Model { get; set; }

            public Shield()
            {
                Type = Types.Shield;
            }
        }

        public class HelmArmorAccessory : Equipment
        {
            [JsonPropertyName("Defense")]
            [JsonPropertyOrder(0)]
            public byte Defense { get; set; }

            [JsonPropertyName("Magick Resist")]
            [JsonPropertyOrder(1)]
            public byte MagickResist { get; set; }

            [JsonPropertyName("Augment")]
            [JsonPropertyOrder(2)]
            public byte Augment { get; set; }

            public HelmArmorAccessory()
            {
                Type = Types.HelmArmorAccessory;
            }
        }

        public class Attribute
        {
            [JsonPropertyName("Max HP")]
            public ushort MaxHp { get; set; }

            [JsonPropertyName("Max MP")]
            public ushort MaxMp { get; set; }

            [JsonPropertyName("Strength")]
            public byte Strength { get; set; }

            [JsonPropertyName("Magick Power")]
            public byte MagickPower { get; set; }

            [JsonPropertyName("Vitality")]
            public byte Vitality { get; set; }

            [JsonPropertyName("Speed")]
            public byte Speed { get; set; }

            [JsonPropertyName("Status Effects")]
            public StatusEffectsEnum StatusEffects { get; set; }

            [JsonPropertyName("Status Effect Immunities")]
            public StatusEffectsEnum StatusEffectImmunities { get; set; }

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
        }

        private class EquipmentConverter : JsonConverter<Dictionary<string, dynamic>>
        {
            public override Dictionary<string, dynamic> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var equipmentDic = new Dictionary<string, dynamic>();
                var jsonDoc = JsonDocument.ParseValue(ref reader);

                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    var typeName = property.Value.GetProperty("Type").ToString();
                    var typeClass = equipmentTypeMap.GetValueOrDefault(typeName);
                    if (typeClass == null)
                    {
                        throw new ArgumentException("Battlepack Section 13: 'Equipment -> Type' is unknown.");
                    }

                    var data = JsonSerializer.Deserialize(property.Value.ToString(), typeClass, options);
                    equipmentDic.Add(property.Name, data);
                }
                return equipmentDic;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, dynamic> data, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, data, options);
            }

            private readonly Dictionary<string, Type> equipmentTypeMap = new()
            {
                { "weapon", typeof(Weapon) },
                { "shield", typeof(Shield) },
                { "ammunition", typeof(Ammunition) },
                { "helmArmorAccessory", typeof(HelmArmorAccessory) },
            };
        }
    }
}