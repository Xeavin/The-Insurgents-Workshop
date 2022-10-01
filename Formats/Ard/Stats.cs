using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Ard
{
    public class Stats : St2e
    {
        [JsonPropertyName("Stats")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Stats(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x38);
        }
        public Stats(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            ReadHeader(br);
            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.ClanPoints = br.ReadUInt16();
                entry.EvadeShield = br.ReadByte();
                entry.MagickEvadeShield = br.ReadByte();
                br.BaseStream.Seek(0x1C, SeekOrigin.Current);
                entry.MaxHp = br.ReadUInt32();
                entry.MaxMp = br.ReadUInt16();
                entry.Strength = br.ReadByte();
                entry.MagickPower = br.ReadByte();
                entry.Vitality = br.ReadByte();
                entry.Speed = br.ReadByte();
                entry.EvadeParry = br.ReadByte();
                entry.Defense = br.ReadByte();
                entry.MagickResist = br.ReadByte();
                entry.AttackPower = br.ReadByte();
                entry.EvadeWeapon = br.ReadByte();
                entry.LicensePoints = br.ReadByte();
                entry.Gil = br.ReadUInt32();
                entry.Experience = br.ReadUInt32();
                Entries.Add($"Stats {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.ClanPoints);
                bw.Write(entry.EvadeShield);
                bw.Write(entry.MagickEvadeShield);
                bw.BaseStream.Seek(0x1C, SeekOrigin.Current);
                bw.Write(entry.MaxHp);
                bw.Write(entry.MaxMp);
                bw.Write(entry.Strength);
                bw.Write(entry.MagickPower);
                bw.Write(entry.Vitality);
                bw.Write(entry.Speed);
                bw.Write(entry.EvadeParry);
                bw.Write(entry.Defense);
                bw.Write(entry.MagickResist);
                bw.Write(entry.AttackPower);
                bw.Write(entry.EvadeWeapon);
                bw.Write(entry.LicensePoints);
                bw.Write(entry.Gil);
                bw.Write(entry.Experience);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Max HP")]
            public uint MaxHp { get; set; }

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

            [JsonPropertyName("Attack Power")]
            public byte AttackPower { get; set; }

            [JsonPropertyName("Defense")]
            public byte Defense { get; set; }

            [JsonPropertyName("Magick Resist")]
            public byte MagickResist { get; set; }

            [JsonPropertyName("Evade (Parry)")]
            public byte EvadeParry { get; set; }

            [JsonPropertyName("Evade (Weapon)")]
            public byte EvadeWeapon { get; set; }

            [JsonPropertyName("Evade (Shield)")]
            public byte EvadeShield { get; set; }

            [JsonPropertyName("Magick Evade (Shield)")]
            public byte MagickEvadeShield { get; set; }

            [JsonPropertyName("Experience")]
            public uint Experience { get; set; }

            [JsonPropertyName("License Points")]
            public byte LicensePoints { get; set; }

            [JsonPropertyName("Clan Points")]
            public ushort ClanPoints { get; set; }

            [JsonPropertyName("Gil")]
            public uint Gil { get; set; }
        }
    }
}
