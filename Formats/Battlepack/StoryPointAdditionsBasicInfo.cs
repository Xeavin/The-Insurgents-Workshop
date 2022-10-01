using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class StoryPointAdditionsBasicInfo : St2e
    {
        [JsonPropertyName("Story Point Additions (Basic Info) List")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public StoryPointAdditionsBasicInfo(Dictionary<string, Entry> entries)
        {
            if (entries.Values.Any(i => i.PartyMembers.Count != 9))
            {
                throw new ArgumentException("Battlepack Section 60: 'Party Members' must contain exactly 9 entries.");
            }

            if (entries.Values.Any(i => i.InventoryEntries.Count != 64))
            {
                throw new ArgumentException("Battlepack Section 60: 'Inventory List' must contain exactly 64 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0xD0);
        }

        public StoryPointAdditionsBasicInfo(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();

                for (var j = 0; j < 4; j++)
                {
                    entry.PartyMembers.Add($"Active Slot {j}", br.ReadSByte());
                }

                for (var j = 0; j < 5; j++)
                {
                    entry.PartyMembers.Add($"Reserve Slot {j}", br.ReadSByte());
                }

                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.ActivePartyLeader = br.ReadByte();
                br.BaseStream.Seek(0x03, SeekOrigin.Current);
                entry.Gil = br.ReadInt16();

                for (var j = 0; j < 64; j++)
                {
                    var inventory = new Inventory
                    {
                        Content = br.ReadUInt16()
                    };
                    entry.InventoryEntries.Add($"Inventory {j}", inventory);
                }

                foreach (var inventory in entry.InventoryEntries.Values)
                {
                    inventory.Quantity = br.ReadByte();
                }
                Entries.Add($"Story Point Addition {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                foreach (var partyMember in entry.PartyMembers.Values)
                {
                    bw.Write(partyMember);
                }

                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.ActivePartyLeader);
                bw.BaseStream.Seek(0x03, SeekOrigin.Current);
                bw.Write(entry.Gil);

                foreach (var inventory in entry.InventoryEntries.Values)
                {
                    bw.Write(inventory.Content);
                }

                bw.Write(entry.InventoryEntries.Values.Select(i => i.Quantity).ToArray());
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Party Members")]
            public Dictionary<string, sbyte> PartyMembers { get; set; }

            [JsonPropertyName("Active Party Leader")]
            public byte ActivePartyLeader { get; set; }

            [JsonPropertyName("Gil")]
            public short Gil { get; set; }

            [JsonPropertyName("Inventory List")]
            public Dictionary<string, Inventory> InventoryEntries { get; set; }

            public Entry()
            {
                PartyMembers = new Dictionary<string, sbyte>();
                InventoryEntries = new Dictionary<string, Inventory>();
            }
        }
        public class Inventory
        {
            [JsonPropertyName("Content")]
            public ushort Content { get; set; }

            [JsonPropertyName("Quantity")]
            public byte Quantity { get; set; }
        }
    }
}