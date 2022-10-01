using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class StoryPointAdditionsExtendedInfo : St2e
    {
        [JsonPropertyName("Story Point Additions (Extended Info) List")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public StoryPointAdditionsExtendedInfo(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x2C);
        }

        public StoryPointAdditionsExtendedInfo(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                entry.BasicInfoLink = br.ReadByte();
                entry.PartyMember = br.ReadByte();
                entry.Level = br.ReadByte();
                entry.MistBars = br.ReadByte();
                entry.Weapon = br.ReadUInt16();
                entry.Offhand = br.ReadUInt16();
                entry.Helm = br.ReadUInt16();
                entry.Armor = br.ReadUInt16();
                entry.Accessory = br.ReadUInt16();
                entry.DefaultPartyMemberGambitsLink = (ushort)(br.ReadUInt16() - 0x5000);
                br.BaseStream.Seek(0x10, SeekOrigin.Current);
                entry.Lp = br.ReadUInt16();
                br.BaseStream.Seek(0x06, SeekOrigin.Current);
                entry.CurrentMpPercentage = br.ReadByte();
                var flags = br.ReadByte();
                entry.GambitState = (flags & 0x01) == 1;
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                Entries.Add($"Story Point Addition {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                byte flags = 0;
                flags |= entry.GambitState ? (byte)0x01 : (byte)0;

                bw.Write(entry.BasicInfoLink);
                bw.Write(entry.PartyMember);
                bw.Write(entry.Level);
                bw.Write(entry.MistBars);
                bw.Write(entry.Weapon);
                bw.Write(entry.Offhand);
                bw.Write(entry.Helm);
                bw.Write(entry.Armor);
                bw.Write(entry.Accessory);
                bw.Write((ushort)(entry.DefaultPartyMemberGambitsLink + 0x5000));
                bw.BaseStream.Seek(0x10, SeekOrigin.Current);
                bw.Write(entry.Lp);
                bw.BaseStream.Seek(0x06, SeekOrigin.Current);
                bw.Write(entry.CurrentMpPercentage);
                bw.Write(flags);
                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Basic Info Link")]
            public byte BasicInfoLink { get; set; }

            [JsonPropertyName("Party Member")]
            public byte PartyMember { get; set; }

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

            [JsonPropertyName("Level")]
            public byte Level { get; set; }

            [JsonPropertyName("LP")]
            public ushort Lp { get; set; }

            [JsonPropertyName("Mist Bars")]
            public byte MistBars { get; set; }

            [JsonPropertyName("Current MP Percentage")]
            public byte CurrentMpPercentage { get; set; }

            [JsonPropertyName("Default Party Member Gambits Link")]
            public ushort DefaultPartyMemberGambitsLink { get; set; }

            [JsonPropertyName("Gambit State")]
            public bool GambitState { get; set; }
        }
    }
}