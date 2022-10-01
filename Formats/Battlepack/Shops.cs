using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Shops
    {
        //magic includes the data length
        private readonly byte[] shopMagic = { 0x64, 0x00, 0x00, 0x00, 0x08, 0x00 };
        private readonly byte[] eventMagic = { 0x64, 0x01, 0x00, 0x00, 0x08, 0x00 };
        private readonly byte[] contentMagic = { 0x64, 0x02, 0x00, 0x00, 0x02, 0x00 };

        [JsonPropertyName("Shops")]
        public Dictionary<string, Shop> Entries { get; set; }

        [JsonConstructor]
        public Shops(Dictionary<string, Shop> entries)
        {
            Entries = entries;
        }

        public Shops(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(6).SequenceEqual(shopMagic))
            {
                throw new ArgumentException("Battlepack Section 39: Unexpected shop magic.");
            }

            br.BaseStream.Seek(0x06, SeekOrigin.Begin);
            var shopCount = br.ReadUInt16();
            br.BaseStream.Seek(br.ReadUInt32(), SeekOrigin.Begin); //skip to shop list

            Entries = new Dictionary<string, Shop>();
            for (var i = 0; i < shopCount; i++)
            {
                var shop = new Shop
                {
                    OwnerName = br.ReadUInt16()
                };

                br.BaseStream.Seek(0x02, SeekOrigin.Current); //skip unused attribute
                var preservedShopPosition = br.BaseStream.Position + 0x04;
                br.BaseStream.Seek(br.ReadUInt32(), SeekOrigin.Begin); //skip to event header

                if (!br.ReadBytes(6).SequenceEqual(eventMagic))
                {
                    throw new ArgumentException("Battlepack Section 39: Unexpected event magic.");
                }

                var eventCount = br.ReadUInt16();
                br.BaseStream.Seek(br.ReadUInt32(), SeekOrigin.Begin); //skip to event list
                for (var j = 0; j < eventCount; j++)
                {
                    var eventEntry = new Event
                    {
                        Condition = br.ReadByte(),
                        ConditionOffsetParameter = br.ReadByte(),
                        ConditionParameter = br.ReadUInt16()
                    };

                    var preservedEventPosition = br.BaseStream.Position + 0x04;
                    br.BaseStream.Seek(br.ReadUInt32(), SeekOrigin.Begin); //skip to content header

                    if (!br.ReadBytes(6).SequenceEqual(contentMagic))
                    {
                        throw new ArgumentException("Battlepack Section 39: Unexpected content magic.");
                    }

                    var contentCount = br.ReadUInt16();
                    br.BaseStream.Seek(br.ReadUInt32(), SeekOrigin.Begin); //skip to content list
                    for (var k = 0; k < contentCount; k++)
                    {
                        eventEntry.Contents.Add(br.ReadUInt16());
                    }
                    br.BaseStream.Position = preservedEventPosition;
                    shop.Events.Add($"Event {j}", eventEntry);
                }
                br.BaseStream.Position = preservedShopPosition;
                Entries.Add($"Shop {i}", shop);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));

            //write header
            bw.Write(shopMagic);
            bw.Write((ushort)Entries.Count);
            bw.Write(0x0C); //shop data start offset

            foreach (var shop in Entries.Values)
            {
                bw.Write(shop.OwnerName);
                bw.BaseStream.Seek(0x06, SeekOrigin.Current); //skip unused attribute + preserve space for header data offsets.
            }

            var eventHeaderOffsets = new List<uint>();
            foreach (var shop in Entries.Values)
            {
                eventHeaderOffsets.Add((uint)bw.BaseStream.Position);
                bw.Write(eventMagic);
                bw.Write((ushort)shop.Events.Count);
                bw.Write((uint)(bw.BaseStream.Position + 0x04)); //events offset

                var preservedEventHeaderPosition = bw.BaseStream.Position;
                foreach (var eventEntry in shop.Events.Values)
                {
                    bw.Write(eventEntry.Condition);
                    bw.Write(eventEntry.ConditionOffsetParameter);
                    bw.Write(eventEntry.ConditionParameter);
                    bw.BaseStream.Seek(0x04, SeekOrigin.Current); //preserve space for content header offsets.
                }

                var contentHeaderOffsets = new List<uint>();
                foreach (var eventEntry in shop.Events.Values)
                {
                    contentHeaderOffsets.Add((uint)bw.BaseStream.Position);
                    bw.Write(contentMagic);
                    bw.Write((ushort)eventEntry.Contents.Count);
                    bw.Write((uint)(bw.BaseStream.Position + 0x04)); //contents offset

                    foreach (var content in eventEntry.Contents)
                    {
                        bw.Write(content);
                    }
                    BinaryHelper.Align(bw, 4);

                    var preservedContentPosition = bw.BaseStream.Position;
                    bw.BaseStream.Position = preservedEventHeaderPosition;
                    foreach (var offset in contentHeaderOffsets)
                    {
                        bw.BaseStream.Seek(0x04, SeekOrigin.Current); //skip to content header offsets.
                        bw.Write(offset);
                    }
                    bw.BaseStream.Position = preservedContentPosition;
                }
            }
            BinaryHelper.Align(bw, 16);

            //write event header offsets
            bw.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            foreach (var offset in eventHeaderOffsets)
            {
                bw.BaseStream.Seek(0x04, SeekOrigin.Current); //skip to event header offset
                bw.Write(offset);
            }
        }

        public class Shop
        {
            [JsonPropertyName("Owner Name")]
            public ushort OwnerName { get; set; }

            [JsonPropertyName("Events")]
            public Dictionary<string, Event> Events { get; set; }

            public Shop()
            {
                Events = new Dictionary<string, Event>();
            }

            [JsonConstructor]
            public Shop(Dictionary<string, Event> events)
            {
                Events = events;
            }
        }
        public class Event
        {
            [JsonPropertyName("Condition")]
            public byte Condition { get; set; }

            [JsonPropertyName("Condition Offset Parameter")]
            public byte ConditionOffsetParameter { get; set; }

            [JsonPropertyName("Condition Parameter")]
            public ushort ConditionParameter { get; set; }

            [JsonPropertyName("Contents")]
            public List<ushort> Contents { get; set; }

            public Event()
            {
                Contents = new List<ushort>();
            }
        }
    }
}
