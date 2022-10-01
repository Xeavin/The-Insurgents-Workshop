using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Ard
{
    public class Units : St2e
    {
        [JsonPropertyName("Unit List")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Units(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x58);
        }

        public Units(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            ReadHeader(br);
            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.ClassLink = br.ReadUInt16();
                entry.GroupIdentifier = br.ReadByte();
                var flags = br.ReadByte();
                entry.HealthBarType = (byte)(flags & 0x03);
                entry.IsHunt = (flags >> 4 & 0x01) == 1;
                entry.IsBoss = (flags >> 5 & 0x01) == 1;
                br.BaseStream.Seek(0x04, SeekOrigin.Current);
                entry.Name = br.ReadUInt16();
                entry.SizeX = br.ReadUInt16();
                entry.SizeY = br.ReadUInt16();
                entry.SizeZ = br.ReadUInt16();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.ModelVariationIdentifier = br.ReadByte();
                entry.ModelColorVariationIdentifier = br.ReadByte();
                entry.ForcedWeaponStanceAnimation = br.ReadByte();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                flags = br.ReadByte();
                entry.AttackPowerAndEvadeWeaponSource = (flags & 0x01) == 1;
                entry.HasShield = (flags >> 1 & 0x01) == 1;
                entry.HasCustomHealthBar = (flags >> 2 & 0x01) == 1;
                entry.UnknownFlag0 = (flags >> 3 & 0x01) == 1;
                entry.UnknownFlag1 = (flags >> 4 & 0x01) == 1;
                entry.Weapon = br.ReadUInt16();
                entry.CustomInitialHp = br.ReadUInt32();
                entry.Offhand = br.ReadUInt16();
                br.BaseStream.Seek(0x04, SeekOrigin.Current);
                entry.DefaultStatsLink = br.ReadUInt16();
                entry.AdditiveStatsLink = br.ReadUInt16();
                entry.Unknown0 = br.ReadInt16();
                entry.CommonDrop = br.ReadUInt16();
                entry.UncommonDrop = br.ReadUInt16();
                entry.RareDrop = br.ReadUInt16();
                entry.VeryRareDrop = br.ReadUInt16();
                entry.GuaranteedDrop = br.ReadUInt16();
                entry.CommonSteal = br.ReadUInt16();
                entry.UncommonSteal = br.ReadUInt16();
                entry.RareSteal = br.ReadUInt16();
                entry.FirstOverlayModel = br.ReadInt32();
                entry.SecondOverlayModel = br.ReadInt32();
                entry.Unknown1 = br.ReadByte();
                entry.Unknown2 = br.ReadByte();
                entry.MonographRate = br.ReadByte();
                entry.CanopicJarRate = br.ReadByte();
                entry.CommonPoach = br.ReadUInt16();
                entry.UncommonPoach = br.ReadUInt16();
                entry.MonographType = br.ReadUInt16();
                entry.MonographDrop = br.ReadUInt16();
                entry.CanopicJarType = br.ReadUInt16();
                entry.CanopicJarDrop = br.ReadUInt16();
                entry.AiScriptLink0 = br.ReadUInt16();
                entry.AiScriptLink1 = br.ReadUInt16();
                entry.AiScriptLink2 = br.ReadUInt16();
                entry.AiScriptLink3 = br.ReadUInt16();
                Entries.Add($"Unit {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                byte firstFlags = 0, secondFlags = 0;

                firstFlags |= entry.HealthBarType;
                firstFlags |= entry.IsHunt ? (byte)0x10 : (byte)0;
                firstFlags |= entry.IsBoss ? (byte)0x20 : (byte)0;
                secondFlags |= entry.AttackPowerAndEvadeWeaponSource ? (byte)0x01 : (byte)0;
                secondFlags |= entry.HasShield ? (byte)0x02 : (byte)0;
                secondFlags |= entry.HasCustomHealthBar ? (byte)0x04 : (byte)0;
                secondFlags |= entry.UnknownFlag0 ? (byte)0x08 : (byte)0;
                secondFlags |= entry.UnknownFlag1 ? (byte)0x10 : (byte)0;
                bw.Write(entry.ClassLink);
                bw.Write(entry.GroupIdentifier);
                bw.Write(firstFlags);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                bw.Write(entry.Name);
                bw.Write(entry.SizeX);
                bw.Write(entry.SizeY);
                bw.Write(entry.SizeZ);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.ModelVariationIdentifier);
                bw.Write(entry.ModelColorVariationIdentifier);
                bw.Write(entry.ForcedWeaponStanceAnimation);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(secondFlags);
                bw.Write(entry.Weapon);
                bw.Write(entry.CustomInitialHp);
                bw.Write(entry.Offhand);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current);
                bw.Write(entry.DefaultStatsLink);
                bw.Write(entry.AdditiveStatsLink);
                bw.Write(entry.Unknown0);
                bw.Write(entry.CommonDrop);
                bw.Write(entry.UncommonDrop);
                bw.Write(entry.RareDrop);
                bw.Write(entry.VeryRareDrop);
                bw.Write(entry.GuaranteedDrop);
                bw.Write(entry.CommonSteal);
                bw.Write(entry.UncommonSteal);
                bw.Write(entry.RareSteal);
                bw.Write(entry.FirstOverlayModel);
                bw.Write(entry.SecondOverlayModel);
                bw.Write(entry.Unknown1);
                bw.Write(entry.Unknown2);
                bw.Write(entry.MonographRate);
                bw.Write(entry.CanopicJarRate);
                bw.Write(entry.CommonPoach);
                bw.Write(entry.UncommonPoach);
                bw.Write(entry.MonographType);
                bw.Write(entry.MonographDrop);
                bw.Write(entry.CanopicJarType);
                bw.Write(entry.CanopicJarDrop);
                bw.Write(entry.AiScriptLink0);
                bw.Write(entry.AiScriptLink1);
                bw.Write(entry.AiScriptLink2);
                bw.Write(entry.AiScriptLink3);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Class Link")]
            public ushort ClassLink { get; set; }

            [JsonPropertyName("Default Stats Link")]
            public ushort DefaultStatsLink { get; set; }

            [JsonPropertyName("Additive Stats Link")]
            public ushort AdditiveStatsLink { get; set; }

            [JsonPropertyName("AI Script Link 0")]
            public ushort AiScriptLink0 { get; set; }

            [JsonPropertyName("AI Script Link 1")]
            public ushort AiScriptLink1 { get; set; }

            [JsonPropertyName("AI Script Link 2")]
            public ushort AiScriptLink2 { get; set; }

            [JsonPropertyName("AI Script Link 3")]
            public ushort AiScriptLink3 { get; set; }

            private byte groupIdentifier;
            [JsonPropertyName("Group Identifier?")]
            public byte GroupIdentifier
            {
                get => groupIdentifier;
                set
                {
                    if (value > 127 && value != 255)
                    {
                        throw new ArgumentException("Ard Section 4: 'Group Identifier' must be 255 or lower than 128.");
                    }
                    groupIdentifier = value;
                }
            }

            [JsonPropertyName("Name")]
            public ushort Name { get; set; }

            [JsonPropertyName("Weapon")]
            public ushort Weapon { get; set; }

            [JsonPropertyName("Off-hand")]
            public ushort Offhand { get; set; }

            [JsonPropertyName("Custom Initial HP")]
            public uint CustomInitialHp { get; set; }

            [JsonPropertyName("Forced Weapon Stance Animation")]
            public byte ForcedWeaponStanceAnimation { get; set; }

            [JsonPropertyName("Model Variation Identifier")]
            public byte ModelVariationIdentifier { get; set; }

            [JsonPropertyName("Model Color Variation Identifier")]
            public byte ModelColorVariationIdentifier { get; set; }

            [JsonPropertyName("Size X")]
            public ushort SizeX { get; set; }

            [JsonPropertyName("Size Y")]
            public ushort SizeY { get; set; }

            [JsonPropertyName("Size Z")]
            public ushort SizeZ { get; set; }

            [JsonPropertyName("First Overlay Model")]
            public int FirstOverlayModel { get; set; }

            [JsonPropertyName("Second Overlay Model")]
            public int SecondOverlayModel { get; set; }

            [JsonPropertyName("Common Drop")]
            public ushort CommonDrop { get; set; }

            [JsonPropertyName("Uncommon Drop")]
            public ushort UncommonDrop { get; set; }

            [JsonPropertyName("Rare Drop")]
            public ushort RareDrop { get; set; }

            [JsonPropertyName("Very Rare Drop")]
            public ushort VeryRareDrop { get; set; }

            [JsonPropertyName("Guaranteed Drop")]
            public ushort GuaranteedDrop { get; set; }

            [JsonPropertyName("Common Steal")]
            public ushort CommonSteal { get; set; }

            [JsonPropertyName("Uncommon Steal")]
            public ushort UncommonSteal { get; set; }

            [JsonPropertyName("Rare Steal")]
            public ushort RareSteal { get; set; }

            [JsonPropertyName("Common Poach")]
            public ushort CommonPoach { get; set; }

            [JsonPropertyName("Uncommon Poach")]
            public ushort UncommonPoach { get; set; }

            [JsonPropertyName("Monograph Rate")]
            public byte MonographRate { get; set; }

            [JsonPropertyName("Monograph Type")]
            public ushort MonographType { get; set; }

            [JsonPropertyName("Monograph Drop")]
            public ushort MonographDrop { get; set; }

            [JsonPropertyName("Canopic Jar Rate")]
            public byte CanopicJarRate { get; set; }

            [JsonPropertyName("Canopic Jar Type")]
            public ushort CanopicJarType { get; set; }

            [JsonPropertyName("Canopic Jar Drop")]
            public ushort CanopicJarDrop { get; set; }

            private byte healthBarType;
            [JsonPropertyName("Health Bar Type?")]
            public byte HealthBarType
            {
                get => healthBarType;
                set
                {
                    if (value > 3)
                    {
                        throw new ArgumentException("Ard Section 4: 'Health Bar Type' cannot be higher than 3.");
                    }
                    healthBarType = value;
                }
            }

            [JsonPropertyName("Is Hunt?")]
            public bool IsHunt { get; set; }

            [JsonPropertyName("Is Boss?")]
            public bool IsBoss { get; set; }

            [JsonPropertyName("Attack Power And Evade (Weapon) Source")]
            public bool AttackPowerAndEvadeWeaponSource { get; set; }

            [JsonPropertyName("Has Shield")]
            public bool HasShield { get; set; }

            [JsonPropertyName("Has Custom Health Bar")]
            public bool HasCustomHealthBar { get; set; }

            [JsonPropertyName("Unknown 0")]
            public short Unknown0 { get; set; }

            [JsonPropertyName("Unknown 1")]
            public byte Unknown1 { get; set; }

            [JsonPropertyName("Unknown 2")]
            public byte Unknown2 { get; set; }

            [JsonPropertyName("Unknown Flag 0")]
            public bool UnknownFlag0 { get; set; }

            [JsonPropertyName("Unknown Flag 1")]
            public bool UnknownFlag1 { get; set; }
        }
    }
}
