using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Formats
{
    public class Tim2
    {
        private readonly byte[] magic = { 0x54, 0x49, 0x4D, 0x32 }; //TIM2

        [JsonPropertyName("File Format Revision")]
        public byte FileFormatRevision { get; set; }

        [JsonPropertyName("Format")]
        public byte Format { get; set; }

        [JsonPropertyName("Picture")]
        public Picture Picture { get; set; }

        [JsonConstructor]
        public Tim2(Picture picture, byte fileFormatRevision, byte format)
        {
            FileFormatRevision = fileFormatRevision;
            Format = format;
            Picture = picture;
        }

        public Tim2(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(4).SequenceEqual(magic))
            {
                throw new ArgumentException("Tim2: Unexpected magic.");
            }

            FileFormatRevision = br.ReadByte();
            Format = br.ReadByte();
            br.BaseStream.Seek(0x0A, SeekOrigin.Current); //skip picture count and reserved 8 bytes
            var pictureBaseOffset = br.BaseStream.Position;

            //read picture header
            br.BaseStream.Seek(0x08, SeekOrigin.Current); //skip total size and clut size
            var imageSize = br.ReadUInt32();
            var headerSize = br.ReadUInt16();
            var clutCount = (ushort)(br.ReadInt16() + 0x000000FF >> 8);

            Picture = new Picture
            {
                PictureFormat = br.ReadByte(),
                MipmapCount = br.ReadByte(),
                ClutColorType = br.ReadByte(),
                ImageColorType = br.ReadByte(),
                ImageWidth = br.ReadUInt16(),
                ImageHeight = br.ReadUInt16(),
                GsTexRegister1 = br.ReadUInt64(),
                GsTexRegister2 = br.ReadUInt64(),
                GsFlagsRegister = br.ReadUInt32(),
                GsClutRegister = br.ReadUInt32()
            };
            var extHeaderBaseOffset = br.BaseStream.Position;

            //read image and cluts
            br.BaseStream.Seek(pictureBaseOffset + headerSize, SeekOrigin.Begin); //tm2 header size + picture header size
            Picture.Image = br.ReadBytes((int)imageSize);
            br.BaseStream.Seek(pictureBaseOffset + headerSize + imageSize, SeekOrigin.Begin);
            for (var i = 0; i < clutCount; i++)
            {
                var clut = new Clt2();

                for (var j = 0; j < 256; j++)
                {
                    clut.Colors.Add(new Rgba
                    {
                        Red = br.ReadByte(),
                        Green = br.ReadByte(),
                        Blue = br.ReadByte(),
                        Alpha = br.ReadByte(),
                    });
                }
                Picture.Cluts.Add(clut);
            }

            br.BaseStream.Position = extHeaderBaseOffset;
            if (!br.ReadBytes(4).SequenceEqual(ExtHeader.Magic))
            {
                return;
            }

            Picture.ExtHeader = new ExtHeader();
            var eXtHeaderSize = br.ReadInt32();
            br.BaseStream.Seek(0x08, SeekOrigin.Current); //skip duplicate header size and reserved 4 bytes
            var iconEntryListOffset = br.ReadInt16();
            var clutListOffset = br.ReadInt16();
            var eXtClutCount = iconEntryListOffset - clutListOffset;

            var iconSectionListOffset = br.BaseStream.Position;
            var iconSectionCount = br.ReadInt16();

            //read eXt header cluts
            br.BaseStream.Position = extHeaderBaseOffset + clutListOffset;
            Picture.ExtHeader.ClutGroups = br.ReadBytes(eXtClutCount).ToList();

            var clutListStart = extHeaderBaseOffset + clutListOffset;
            var imageListStart = extHeaderBaseOffset + eXtHeaderSize;
            for (var i = 0; i < iconSectionCount; i++)
            {
                br.BaseStream.Position = iconSectionListOffset + 2 * i;

                var groupCount = 0;
                var curGroupOffset = br.ReadUInt16();
                if (i != iconSectionCount - 1)
                {
                    var nextGroupOffset = br.ReadUInt16();
                    groupCount = nextGroupOffset - curGroupOffset;
                }
                else
                {
                    //since the last section has no end offset, calculate set count manually.
                    br.BaseStream.Position = iconSectionListOffset + 2 * curGroupOffset;
                    while (br.BaseStream.Position < clutListStart)
                    {
                        var temp = br.ReadUInt16();
                        if (temp == 0 && groupCount != 0) //in case there is only one section
                        {
                            break;
                        }
                        groupCount++;
                    }
                }

                var iconSection = new IconSection();
                for (var j = 0; j < groupCount; j++)
                {
                    br.BaseStream.Position = iconSectionListOffset + 2 * curGroupOffset + 2 * j;
                    var curIconListOffset = br.ReadInt16();
                    var iconCount = 0;
                    if (i != iconSectionCount - 1 || j != groupCount - 1)
                    {
                        var nextIconListOffset = br.ReadInt16();
                        iconCount = nextIconListOffset - curIconListOffset;
                    }
                    else
                    {
                        //since the last set has no end offset, calculate icon count manually.
                        br.BaseStream.Position = extHeaderBaseOffset + iconEntryListOffset + 8 * curIconListOffset;
                        while (br.BaseStream.Position < imageListStart)
                        {
                            var temp = br.ReadUInt64();
                            if (temp == 0 && iconCount != 0)
                            {
                                break;
                            }
                            iconCount++;
                        }
                    }

                    br.BaseStream.Position = extHeaderBaseOffset + iconEntryListOffset + 8 * curIconListOffset;
                    var iconGroup = new IconGroup();
                    for (var k = 0; k < iconCount; k++)
                    {
                        var icon = new IconEntry();
                        icon.X = br.ReadUInt16();
                        icon.Y = br.ReadUInt16();
                        var flags = br.ReadUInt32();
                        icon.Width = (ushort)(flags & 0x0FFF);
                        icon.Height = (ushort)(flags >> 12 & 0x0FFF);
                        icon.ClutGroupLink = (byte)(flags >> 28); //24 to get byte, 4 to get clut group link
                        icon.AdditiveClutEntryLink = (byte)(flags >> 24 & 0x0F);
                        iconGroup.Icons.Add($"Icon {k}", icon);
                    }
                    iconSection.IconGroups.Add($"Icon Group {j}", iconGroup);
                }
                Picture.ExtHeader.IconSections.Add($"Icon Section {i}", iconSection);
            }
        }

        public void WriteMultiLayerBinary(string inputPath, string outputPath)
        {
            var mergedPath = Path.Combine(inputPath, "merged.raw");
            Picture.Image = File.ReadAllBytes(mergedPath);

            uint totalSize;
            uint clutSize;

            var layersPath = Path.Combine(inputPath, "layers");
            var layerFilePaths = Directory.GetFiles(layersPath, "*.tm2", SearchOption.TopDirectoryOnly);
            foreach (var layer in layerFilePaths)
            {
                using var br = new BinaryReader(File.Open(layer, FileMode.Open));
                if (!br.ReadBytes(4).SequenceEqual(magic))
                {
                    throw new ArgumentException("Tim2: Unexpected magic.");
                }

                br.BaseStream.Seek(0x10, SeekOrigin.Begin); //skip tm2 header
                totalSize = br.ReadUInt32();
                clutSize = br.ReadUInt32();
                br.BaseStream.Seek(0x10 + totalSize - clutSize, SeekOrigin.Begin); //skip to clut

                var clut = new Clt2();
                for (var i = 0; i < clutSize / 4; i++)
                {
                    clut.Colors.Add(new Rgba
                    {
                        Red = br.ReadByte(),
                        Green = br.ReadByte(),
                        Blue = br.ReadByte(),
                        Alpha = br.ReadByte()
                    });
                }
                Picture.Cluts.Add(clut);
            }

            using var bw = new BinaryWriter(File.Open(outputPath, FileMode.Create));
            bw.BaseStream.Seek(0x40, SeekOrigin.Begin); //skip tm2 header and image header

            //write icon section, set offsets, and their entries.
            ushort clutListOffset = 0;
            ushort iconListOffset = 0;
            if (Picture.ExtHeader != null)
            {
                bw.BaseStream.Seek(0x14, SeekOrigin.Current); //go to icon section list of eXt header
                var curIconSectionOffset = (ushort)Picture.ExtHeader.IconSections.Count;
                foreach (var iconSection in Picture.ExtHeader.IconSections.Values)
                {
                    bw.Write(curIconSectionOffset);
                    curIconSectionOffset += (ushort)iconSection.IconGroups.Count;
                }

                var curIconGroupOffset = (ushort)0;
                var icons = new List<IconEntry>();
                foreach (var iconGroup in Picture.ExtHeader.IconSections.Values.SelectMany(iconSection => iconSection.IconGroups.Values))
                {
                    bw.Write(curIconGroupOffset);
                    curIconGroupOffset += (ushort)iconGroup.Icons.Count;
                    icons.AddRange(iconGroup.Icons.Values);
                }

                BinaryHelper.Align(bw, 16);
                clutListOffset = (ushort)(bw.BaseStream.Position - 0x40); //subtract tm2 and picture header size

                bw.Write(Picture.ExtHeader.ClutGroups.ToArray());
                iconListOffset = (ushort)(bw.BaseStream.Position - 0x40); //subtract tm2 and picture header size

                foreach (var icon in icons)
                {
                    bw.Write(icon.X);
                    bw.Write(icon.Y);
                    var flags = (uint)(icon.Width | icon.Height << 12 | icon.ClutGroupLink << 28 | icon.AdditiveClutEntryLink << 24);
                    bw.Write(flags);
                }
                BinaryHelper.Align(bw, 16);
            }

            var headerSize = (ushort)(bw.BaseStream.Position - 0x10); //subtract tm2 header size
            var eXtHeaderSize = headerSize - 0x30;

            //write image
            bw.Write(Picture.Image);

            //write cluts
            foreach (var color in Picture.Cluts.SelectMany(clut => clut.Colors))
            {
                bw.Write(color.Red);
                bw.Write(color.Green);
                bw.Write(color.Blue);
                bw.Write((byte)((color.Alpha + 2 - 1) / 2)); //halve alpha (round up) as ps2 doubles it.
            }

            //write tm2 header
            bw.BaseStream.Position = 0;
            bw.Write(magic);
            bw.Write(FileFormatRevision);
            bw.Write(Format);
            bw.Write((short)1); //picture count
            bw.BaseStream.Seek(0x08, SeekOrigin.Current); //skip reserved 8 bytes

            //prepare offsets for image header
            clutSize = (uint)(1024 * Picture.Cluts.Count);
            var imageSize = Picture.Image.Length;
            totalSize = (uint)(clutSize + imageSize + headerSize);
            var clutCount = (ushort)(Picture.Cluts.Count << 8);

            //write picture header
            bw.Write(totalSize);
            bw.Write(clutSize);
            bw.Write(imageSize);
            bw.Write(headerSize);
            bw.Write(clutCount);
            bw.Write(Picture.PictureFormat);
            bw.Write((byte)1); //mipmap count
            bw.Write(Picture.ClutColorType);
            bw.Write(Picture.ImageColorType);
            bw.Write(Picture.ImageWidth);
            bw.Write(Picture.ImageHeight);
            bw.Write(Picture.GsTexRegister1);
            bw.Write(Picture.GsTexRegister2);
            bw.Write(Picture.GsFlagsRegister);
            bw.Write(Picture.GsClutRegister);

            //write eXt header
            if (Picture.ExtHeader != null)
            {
                bw.Write(ExtHeader.Magic);
                bw.Write(eXtHeaderSize);
                bw.Write(eXtHeaderSize);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current); //skip reserved 4 bytes
                bw.Write(iconListOffset);
                bw.Write(clutListOffset);
            }
        }

        public void WriteSingleLayerBinary(string path, JsonSerializerOptions options)
        {
            var layersPath = Path.Combine(path, "layers");
            Directory.CreateDirectory(layersPath);

            for (var i = 0; i < Picture.Cluts.Count; i++)
            {
                var layerPath = Path.Combine(layersPath, $"section_{i:000}.tm2");
                using var bw = new BinaryWriter(File.Open(layerPath, FileMode.Create));
                bw.BaseStream.Seek(0x40, SeekOrigin.Begin); //skip tm2 header and image header

                var headerSize = (ushort)(bw.BaseStream.Position - 0x10); //subtract tm2 header size
                bw.Write(Picture.Image);

                //write cluts
                foreach (var color in Picture.Cluts[i].Colors)
                {
                    bw.Write(color.Red);
                    bw.Write(color.Green);
                    bw.Write(color.Blue);
                    bw.Write((byte)(color.Alpha * 2 > 255 ? 255 : color.Alpha * 2)); //double alpha as ps2 uses halved alphas.
                }

                //write tm2 header
                bw.BaseStream.Position = 0;
                bw.Write(magic);
                bw.Write(FileFormatRevision);
                bw.Write(Format);
                bw.Write((short)1); //picture count
                bw.BaseStream.Seek(0x08, SeekOrigin.Current); //skip reserved 8 bytes

                //prepare offsets for image header
                var clutSize = 1024;
                var imageSize = Picture.Image.Length;
                var totalSize = clutSize + imageSize + headerSize;

                //write picture header
                bw.Write(totalSize);
                bw.Write(clutSize);
                bw.Write(imageSize);
                bw.Write(headerSize);
                bw.Write((ushort)(1 << 8)); //clut count
                bw.Write(Picture.PictureFormat);
                bw.Write((byte)1); //mipmap count
                bw.Write(Picture.ClutColorType);
                bw.Write(Picture.ImageColorType);
                bw.Write(Picture.ImageWidth);
                bw.Write(Picture.ImageHeight);
                bw.Write(Picture.GsTexRegister1);
                bw.Write(Picture.GsTexRegister2);
                bw.Write(Picture.GsFlagsRegister);
                bw.Write(Picture.GsClutRegister);
            }

            var mergedPath = Path.Combine(path, "merged.raw");
            using var br = new BinaryWriter(File.Open(mergedPath, FileMode.Create));
            br.Write(Picture.Image);

            var jsonContent = JsonSerializer.Serialize(this, options);
            var jsonPath = Path.Combine(path, "info.json");
            File.WriteAllText(jsonPath, jsonContent);
        }
    }

    public class Picture
    {
        [JsonPropertyName("Picture Format")]
        public byte PictureFormat { get; set; }

        [JsonPropertyName("Mipmap Count")]
        public byte MipmapCount { get; set; }

        [JsonPropertyName("Clut Color Type")]
        public byte ClutColorType { get; set; }

        [JsonPropertyName("Image Color Type")]
        public byte ImageColorType { get; set; }

        [JsonPropertyName("Image Width")]
        public ushort ImageWidth { get; set; }

        [JsonPropertyName("Image Height")]
        public ushort ImageHeight { get; set; }

        [JsonPropertyName("Gs Tex Register 1")]
        public ulong GsTexRegister1 { get; set; }

        [JsonPropertyName("Gs Tex Register 2")]
        public ulong GsTexRegister2 { get; set; }

        [JsonPropertyName("Gs Flags Register")]
        public uint GsFlagsRegister { get; set; }

        [JsonPropertyName("Gs Clut Register")]
        public uint GsClutRegister { get; set; }

        [JsonPropertyName("Ext Header")]
        public ExtHeader ExtHeader { get; set; }

        public byte[] Image;

        public List<Clt2> Cluts;

        public Picture()
        {
            Image = Array.Empty<byte>();
            Cluts = new List<Clt2>();
        }
    }

    public class ExtHeader
    {
        public static readonly byte[] Magic = { 0x65, 0x58, 0x74, 0x0 }; //eXt

        [JsonPropertyName("Icon Sections")]
        public Dictionary<string, IconSection> IconSections { get; set; }

        [JsonPropertyName("Clut Groups")]
        public List<byte> ClutGroups { get; set; }

        [JsonConstructor]
        public ExtHeader(Dictionary<string, IconSection> iconSections, List<byte> clutGroups)
        {
            if (clutGroups.Count != 16)
            {
                throw new ArgumentException("Tim2: 'Ext Header - Clut Groups' must contain exactly 16 entries.");
            }

            IconSections = iconSections;
            ClutGroups = clutGroups;
        }

        public ExtHeader()
        {
            IconSections = new Dictionary<string, IconSection>();
            ClutGroups = new List<byte>();
        }
    }

    public class IconSection
    {
        [JsonPropertyName("Icon Groups")]
        public Dictionary<string, IconGroup> IconGroups { get; set; }

        [JsonConstructor]
        public IconSection(Dictionary<string, IconGroup> iconGroups)
        {
            IconGroups = iconGroups;
        }

        public IconSection()
        {
            IconGroups = new Dictionary<string, IconGroup>();
        }
    }

    public class IconGroup
    {
        [JsonPropertyName("Icons")]
        public Dictionary<string, IconEntry> Icons { get; set; }

        [JsonConstructor]
        public IconGroup(Dictionary<string, IconEntry> icons)
        {
            Icons = icons;
        }

        public IconGroup()
        {
            Icons = new Dictionary<string, IconEntry>();
        }
    }

    public class IconEntry
    {
        [JsonPropertyName("X")]
        public ushort X { get; set; }

        [JsonPropertyName("Y")]
        public ushort Y { get; set; }

        [JsonPropertyName("Width")]
        public ushort Width { get; set; }

        [JsonPropertyName("Height")]
        public ushort Height { get; set; }

        private byte clutGroupLink;

        [JsonPropertyName("Clut Group Link")]
        public byte ClutGroupLink
        {
            get => clutGroupLink;
            set
            {
                if (value > 15)
                {
                    throw new ArgumentException("Tm2: 'Icon Entry -> Clut Group Link' can not be higher than 15.");
                }
                clutGroupLink = value;
            }
        }

        private byte additiveClutEntryLink;

        [JsonPropertyName("Additive Clut Entry Link")]
        public byte AdditiveClutEntryLink
        {
            get => additiveClutEntryLink;
            set
            {
                if (value > 15)
                {
                    throw new ArgumentException("Tm2: 'Icon Entry -> Additive Clut Entry Link' can not be higher than 15.");
                }
                additiveClutEntryLink = value;
            }
        }
    }

    public class Clt2
    {
        public List<Rgba> Colors { get; set; }

        public Clt2()
        {
            Colors = new List<Rgba>();
        }
    }
}


