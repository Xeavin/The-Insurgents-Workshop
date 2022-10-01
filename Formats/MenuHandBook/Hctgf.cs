using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.MenuHandBook
{
    public class Hctgf
    {
        private readonly byte[] magic = { 0x68, 0x63, 0x74, 0x67, 0x66, 0x00, 0x00, 0x00 }; //hctgf

        [JsonPropertyName("Type?")]
        public ushort Type { get; set; }

        [JsonPropertyName("Images File Link Offset")]
        public ushort ImagesFileLinkOffset { get; set; }

        [JsonPropertyName("Images File Count")]
        public ushort ImagesFileCount { get; set; }

        [JsonPropertyName("Header Basic Info List")]
        public Dictionary<string, HeaderBasicInfo> HeaderBasicInfoDic { get; set; }

        [JsonPropertyName("Header Extended Info List")]
        public Dictionary<string, HeaderExtendedInfo> HeaderExtendedInfoDic { get; set; }

        [JsonPropertyName("Entry Basic Info List")]
        public Dictionary<string, EntryBasicInfo> EntryBasicInfoDic { get; set; }

        [JsonPropertyName("Entry Extended Info List")]
        public Dictionary<string, EntryExtendedInfo> EntryExtendedInfoDic { get; set; }

        [JsonPropertyName("Entry Footer Info List")]
        public Dictionary<string, EntryFooterInfo> EntryFooterInfoDic { get; set; }

        [JsonConstructor]
        public Hctgf(ushort type, ushort imagesFileLinkOffset, ushort imagesFileCount, Dictionary<string, HeaderBasicInfo> headerBasicInfoDic, Dictionary<string, HeaderExtendedInfo> headerExtendedInfoDic, Dictionary<string, EntryBasicInfo> entryBasicInfoDic, Dictionary<string, EntryExtendedInfo> entryExtendedInfoDic, Dictionary<string, EntryFooterInfo> entryFooterInfoDic)
        {
            if (headerExtendedInfoDic.Values.Any(i => i.Contents.Count > 15))
            {
                throw new ArgumentException("Hctgf: 'Header Extended Info - Contents' cannot contain more than 15 entries.");
            }

            Type = type;
            ImagesFileLinkOffset = imagesFileLinkOffset;
            ImagesFileCount = imagesFileCount;
            HeaderBasicInfoDic = headerBasicInfoDic;
            HeaderExtendedInfoDic = headerExtendedInfoDic;
            EntryBasicInfoDic = entryBasicInfoDic;
            EntryExtendedInfoDic = entryExtendedInfoDic;
            EntryFooterInfoDic = entryFooterInfoDic;
        }

        public Hctgf(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(8).SequenceEqual(magic))
            {
                throw new ArgumentException("Hctgf: Unexpected magic.");
            }

            Type = br.ReadUInt16();
            var entryBasicInfoOffset = br.ReadUInt16();
            var entryExtInfoOffset = br.ReadUInt16();
            var entryBasicInfoCount = br.ReadUInt16();
            var entryExtInfoCount = br.ReadUInt16();
            var headerBasicInfoCount = br.ReadUInt16();
            ImagesFileLinkOffset = br.ReadUInt16();
            ImagesFileCount = br.ReadUInt16();

            HeaderBasicInfoDic = new Dictionary<string, HeaderBasicInfo>();
            for (var i = 0; i < headerBasicInfoCount; i++)
            {
                var hbi = new HeaderBasicInfo
                {
                    Name = br.ReadInt32()
                };
                HeaderBasicInfoDic.Add($"Header Basic Info {i}", hbi);
            }

            var headerExtendedInfoBase = br.BaseStream.Position;
            var headerExtendedInfoCount = br.ReadUInt16();

            var headerExtendedInfoOffsets = new List<ushort>();
            for (var i = 0; i < headerExtendedInfoCount; i++)
            {
                headerExtendedInfoOffsets.Add(br.ReadUInt16());
            }

            HeaderExtendedInfoDic = new Dictionary<string, HeaderExtendedInfo>();
            for (var i = 0; i < headerExtendedInfoOffsets.Count; i++)
            {
                br.BaseStream.Position = headerExtendedInfoBase + headerExtendedInfoOffsets[i];
                var hei = new HeaderExtendedInfo();
                var contentCount = (byte)(br.ReadByte() >> 4 & 0x0F);
                var locationCount = br.ReadByte();
                for (var j = 0; j < contentCount; j++)
                {
                    hei.Contents.Add(br.ReadUInt16());
                }

                for (var j = 0; j < locationCount; j++)
                {
                    hei.Locations.Add(br.ReadUInt16());
                }
                HeaderExtendedInfoDic.Add($"Header Extended Info {i}", hei);
            }

            EntryExtendedInfoDic = new Dictionary<string, EntryExtendedInfo>();
            br.BaseStream.Seek(entryExtInfoOffset, SeekOrigin.Begin);
            for (var i = 0; i < entryExtInfoCount; i++)
            {
                var eei = new EntryExtendedInfo
                {
                    ImageFileLink = br.ReadUInt16()
                };
                EntryExtendedInfoDic.Add($"Entry Extended Info {i}", eei);
            }

            EntryBasicInfoDic = new Dictionary<string, EntryBasicInfo>();
            br.BaseStream.Seek(entryBasicInfoOffset, SeekOrigin.Begin);
            for (var i = 0; i < entryBasicInfoCount; i++)
            {
                var ebi = new EntryBasicInfo();
                ebi.Name = br.ReadInt32();
                ebi.HeaderBasicInfoLink = br.ReadUInt16();
                ebi.TextFileLink = br.ReadUInt16();
                ebi.EntryExtendedInfoLink = br.ReadUInt16();

                var flags = br.ReadByte();
                ebi.AutoUnlock = (flags & 0x01) == 1;
                br.BaseStream.Seek(0x01, SeekOrigin.Current);

                EntryBasicInfoDic.Add($"Entry Basic Info {i}", ebi);
            }

            var entryFooterInfoBase = br.BaseStream.Position;
            var entryFooterInfoCount = br.ReadUInt16();
            var entryFooterInfoOffsets = new List<ushort>();
            for (var i = 0; i < entryFooterInfoCount; i++)
            {
                entryFooterInfoOffsets.Add(br.ReadUInt16());
            }

            EntryFooterInfoDic = new Dictionary<string, EntryFooterInfo>();
            for (var i = 0; i < entryFooterInfoCount; i++)
            {
                br.BaseStream.Position = entryFooterInfoBase + entryFooterInfoOffsets[i];
                var efi = new EntryFooterInfo
                {
                    Count = br.ReadByte()
                };
                EntryFooterInfoDic.Add($"Entry Footer Info {i}", efi);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(magic);
            bw.Write(Type);
            bw.BaseStream.Seek(0x04, SeekOrigin.Current); //skip entry basic info offset and entry extended info offset
            bw.Write((ushort)EntryBasicInfoDic.Count);
            bw.Write((ushort)EntryExtendedInfoDic.Count);
            bw.Write((ushort)HeaderBasicInfoDic.Count);
            bw.Write(ImagesFileLinkOffset);
            bw.Write(ImagesFileCount);

            foreach (var hbi in HeaderBasicInfoDic.Values)
            {
                bw.Write(hbi.Name);
            }

            var headerExtendedInfoBase = bw.BaseStream.Position;
            bw.Write((ushort)HeaderExtendedInfoDic.Count);
            BinaryHelper.Align(bw, 4); //in case header extended info count is 0
            bw.BaseStream.Seek(HeaderExtendedInfoDic.Count * 0x02, SeekOrigin.Current); //write header extended info offsets later

            var headerExtendedInfoOffsets = new List<ushort>();
            foreach (var hei in HeaderExtendedInfoDic.Values)
            {
                headerExtendedInfoOffsets.Add((ushort)(bw.BaseStream.Position - headerExtendedInfoBase));
                bw.Write((byte)(hei.Contents.Count << 4));
                bw.Write((byte)hei.Locations.Count);
                foreach (var content in hei.Contents)
                {
                    bw.Write(content);
                }

                foreach (var location in hei.Locations)
                {
                    bw.Write(location);
                }
                BinaryHelper.Align(bw, 4);
            }

            var entryExtendedInfoOffset = (ushort)bw.BaseStream.Position;
            foreach (var eei in EntryExtendedInfoDic.Values)
            {
                bw.Write(eei.ImageFileLink);
            }
            BinaryHelper.Align(bw, 4);

            var entryBasicInfoOffset = (ushort)bw.BaseStream.Position;
            foreach (var ebi in EntryBasicInfoDic.Values)
            {
                var flags = ebi.AutoUnlock ? (byte)0x01 : (byte)0;
                bw.Write(ebi.Name);
                bw.Write(ebi.HeaderBasicInfoLink);
                bw.Write(ebi.TextFileLink);
                bw.Write(ebi.EntryExtendedInfoLink);
                bw.Write(flags);
                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
            }

            var entryFooterInfoBase = bw.BaseStream.Position;
            bw.Write((ushort)EntryFooterInfoDic.Count);
            bw.BaseStream.Seek(EntryFooterInfoDic.Count * 0x02, SeekOrigin.Current); //write entry footer info offsets later
            BinaryHelper.Align(bw, 16);

            var entryFooterInfoOffsets = new List<ushort>();
            foreach (var efi in EntryFooterInfoDic.Values)
            {
                entryFooterInfoOffsets.Add((ushort)(bw.BaseStream.Position - entryFooterInfoBase));
                bw.Write(efi.Count);
                bw.Write(new byte[7]);
            }
            BinaryHelper.Align(bw, 16);

            bw.BaseStream.Seek(0x0A, SeekOrigin.Begin);
            bw.Write(entryBasicInfoOffset);
            bw.Write(entryExtendedInfoOffset);

            bw.BaseStream.Seek(headerExtendedInfoBase + 0x02, SeekOrigin.Begin);
            foreach (var offset in headerExtendedInfoOffsets)
            {
                bw.Write(offset);
            }

            bw.BaseStream.Seek(entryFooterInfoBase + 0x02, SeekOrigin.Begin);
            foreach (var offset in entryFooterInfoOffsets)
            {
                bw.Write(offset);
            }
        }

        public class HeaderBasicInfo
        {
            [JsonPropertyName("Name")]
            public int Name { get; set; }
        }

        public class HeaderExtendedInfo
        {
            [JsonPropertyName("Contents")]
            public List<ushort> Contents { get; set; }

            [JsonPropertyName("Locations")]
            public List<ushort> Locations { get; set; }

            public HeaderExtendedInfo()
            {
                Contents = new List<ushort>();
                Locations = new List<ushort>();
            }
        }

        public class EntryBasicInfo
        {
            [JsonPropertyName("Name")]
            public int Name { get; set; }

            [JsonPropertyName("Header Basic Info Link")]
            public ushort HeaderBasicInfoLink { get; set; }

            [JsonPropertyName("Text File Link")]
            public ushort TextFileLink { get; set; }

            [JsonPropertyName("Entry Extended Info Link")]
            public ushort EntryExtendedInfoLink { get; set; }

            [JsonPropertyName("Auto Unlock")]
            public bool AutoUnlock { get; set; }
        }

        public class EntryExtendedInfo
        {
            [JsonPropertyName("Image File Link")]
            public ushort ImageFileLink { get; set; } //range: 0->296?
        }

        public class EntryFooterInfo
        {
            private byte count;
            [JsonPropertyName("Count")]
            public byte Count
            {
                get => count;
                set
                {
                    if (value > 7)
                    {
                        throw new ArgumentException("Hctgf: 'Entry Footer Info - Count' must be lower than 8.");
                    }
                    count = value;
                }
            }
        }
    }
}
