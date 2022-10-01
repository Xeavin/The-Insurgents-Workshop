using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Formats
{
    public class Mrp
    {
        private readonly byte[] magic = { 0x4D, 0x52, 0x50 }; //MRP

        [JsonPropertyName("Groups")]
        public Dictionary<string, Group> Groups { get; set; }

        [JsonPropertyName("Textures")]
        public Dictionary<string, Texture> Textures { get; set; }

        [JsonConstructor]
        public Mrp(Dictionary<string, Group> groups, Dictionary<string, Texture> textures)
        {
            Groups = groups;
            Textures = textures;
        }

        public Mrp(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            if (!br.ReadBytes(3).SequenceEqual(magic))
            {
                throw new ArgumentException("Mrp: Unexpected magic.");
            }

            br.BaseStream.Seek(0x01, SeekOrigin.Current); //skip state
            var textureCount = br.ReadUInt32();
            var textureListOffset = br.ReadUInt32();
            br.BaseStream.Seek(textureListOffset, SeekOrigin.Begin);
            Textures = new Dictionary<string, Texture>();
            for (var i = 0; i < textureCount; i++)
            {
                var texture = new Texture();

                var nameBytes = new List<byte>();
                for (var j = 0; j < 14; j++)
                {
                    var charAsByte = br.ReadByte();
                    if (charAsByte != 0)
                    {
                        nameBytes.Add(charAsByte);
                    }
                }

                texture.Name = BinaryHelper.GetEncodedStringByBytes(nameBytes.ToArray());
                br.BaseStream.Seek(0x02, SeekOrigin.Current); //skip state and link
                Textures.Add($"Texture {i}", texture);
            }

            br.BaseStream.Seek(0x1C, SeekOrigin.Begin);
            var groupCount = br.ReadUInt32();
            var groupListOffset = br.ReadUInt32();
            br.BaseStream.Seek(groupListOffset, SeekOrigin.Begin);
            Groups = new Dictionary<string, Group>();
            for (var i = 0; i < groupCount; i++)
            {
                var groupPosition = (uint)br.BaseStream.Position;
                var group = new Group();
                var entryCount = br.ReadByte();
                br.BaseStream.Seek(0x02, SeekOrigin.Current); //skip index
                var groupFlags = br.ReadByte();
                group.IsVisible = (groupFlags >> 4 & 0x01) == 1;
                group.X = br.ReadInt16();
                group.Y = br.ReadInt16();
                group.Width = br.ReadUInt16();
                group.Height = br.ReadUInt16();
                group.Gap = br.ReadInt32();
                var entryListOffset = br.ReadUInt32() + groupPosition;

                var preservedPosition = br.BaseStream.Position;
                br.BaseStream.Seek(entryListOffset, SeekOrigin.Begin); //entry list offset
                for (var j = 0; j < entryCount; j++)
                {
                    dynamic entry;
                    br.BaseStream.Seek(0x01, SeekOrigin.Current); //skip size
                    var type = br.ReadByte();
                    br.BaseStream.Seek(0x01, SeekOrigin.Current); //skip index

                    switch (type)
                    {
                        case 1:
                            {
                                entry = new EntryType1();
                                var entryFlags = br.ReadByte();
                                entry.IsVisible = (entryFlags >> 4 & 0x01) == 1;
                                entry.X = br.ReadInt16();
                                entry.Y = br.ReadInt16();
                                entry.Width = br.ReadUInt16();
                                entry.Height = br.ReadUInt16();
                                entry.GroupLink = br.ReadUInt32();
                                break;
                            }
                        case 2:
                            {
                                entry = new EntryType2();
                                var entryFlags = br.ReadByte();
                                entry.IsVisible = (entryFlags >> 4 & 0x01) == 1;
                                entry.X = br.ReadInt16();
                                entry.Y = br.ReadInt16();
                                entry.Width = br.ReadUInt16();
                                entry.Height = br.ReadUInt16();
                                entry.TextureFileLink = br.ReadUInt16();
                                entry.IconGroupLink = br.ReadByte();
                                entry.IconSectionLink = br.ReadByte();
                                entry.IconEntryLink = br.ReadUInt16();
                                entry.CustomClutLink = br.ReadUInt16();
                                entry.ColorsTopLeft.Red = br.ReadByte();
                                entry.ColorsTopLeft.Green = br.ReadByte();
                                entry.ColorsTopLeft.Blue = br.ReadByte();
                                entry.ColorsTopLeft.Alpha = br.ReadByte();
                                entry.ColorsTopRight.Red = br.ReadByte();
                                entry.ColorsTopRight.Green = br.ReadByte();
                                entry.ColorsTopRight.Blue = br.ReadByte();
                                entry.ColorsTopRight.Alpha = br.ReadByte();
                                entry.ColorsBottomLeft.Red = br.ReadByte();
                                entry.ColorsBottomLeft.Green = br.ReadByte();
                                entry.ColorsBottomLeft.Blue = br.ReadByte();
                                entry.ColorsBottomLeft.Alpha = br.ReadByte();
                                entry.ColorsBottomRight.Red = br.ReadByte();
                                entry.ColorsBottomRight.Green = br.ReadByte();
                                entry.ColorsBottomRight.Blue = br.ReadByte();
                                entry.ColorsBottomRight.Alpha = br.ReadByte();
                                break;
                            }
                        case 4:
                            {
                                entry = new EntryType4();
                                var entryFlags = br.ReadByte();
                                entry.IsVisible = (entryFlags >> 4 & 0x01) == 1;
                                entry.X = br.ReadInt16();
                                entry.Y = br.ReadInt16();
                                entry.Width = br.ReadUInt16();
                                entry.Height = br.ReadUInt16();
                                br.BaseStream.Seek(0x10, SeekOrigin.Current);
                                break;
                            }
                        case 5:
                            {
                                entry = new EntryType5();
                                var entryFlags = br.ReadByte();
                                entry.IsVisible = (entryFlags >> 4 & 0x01) == 1;
                                entry.X = br.ReadInt16();
                                entry.Y = br.ReadInt16();
                                entry.Width = br.ReadUInt16();
                                entry.Height = br.ReadUInt16();
                                entry.Colors.Red = br.ReadByte();
                                entry.Colors.Green = br.ReadByte();
                                entry.Colors.Blue = br.ReadByte();
                                entry.Colors.Alpha = br.ReadByte();
                                entry.Scale = br.ReadByte();
                                entry.TextStyle = br.ReadByte();
                                entry.Unknown = br.ReadUInt16();
                                entry.TextLink = br.ReadInt32();
                                break;
                            }
                        case 6:
                            {
                                entry = new EntryType6();
                                var entryFlags = br.ReadByte();
                                entry.IsVisible = (entryFlags >> 4 & 0x01) == 1;
                                entry.X = br.ReadInt16();
                                entry.Y = br.ReadInt16();
                                entry.Width = br.ReadUInt16();
                                entry.Height = br.ReadUInt16();
                                br.BaseStream.Seek(0x0C, SeekOrigin.Current);
                                break;
                            }
                        case 7:
                            {
                                entry = new EntryType7();
                                var entryFlags = br.ReadByte();
                                entry.IsVisible = (entryFlags >> 4 & 0x01) == 1;
                                entry.X = br.ReadInt16();
                                entry.Y = br.ReadInt16();
                                entry.Width = br.ReadUInt16();
                                entry.Height = br.ReadUInt16();
                                entry.TextureFileLink = br.ReadUInt16();
                                entry.IconGroupLink = br.ReadByte();
                                entry.IconSectionLink = br.ReadByte();
                                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                                entry.Speed = br.ReadByte();
                                entryFlags = br.ReadByte();
                                entry.UnknownFlag0 = (entryFlags >> 1 & 0x01) == 1;
                                entry.BackgroundLayer.X = br.ReadInt16();
                                entry.BackgroundLayer.Y = br.ReadInt16();
                                entry.BackgroundLayer.OuterLeftIconGroupLink = br.ReadByte();
                                entry.BackgroundLayer.OuterLeftIconSectionLink = br.ReadByte();
                                entry.BackgroundLayer.MiddleIconGroupLink = br.ReadByte();
                                entry.BackgroundLayer.MiddleIconSectionLink = br.ReadByte();
                                entry.BackgroundLayer.OuterRightIconGroupLink = br.ReadByte();
                                entry.BackgroundLayer.OuterRightIconSectionLink = br.ReadByte();
                                entry.BackgroundLayer.CustomClutLink = br.ReadUInt16();
                                entry.BackgroundLayer.Width = br.ReadUInt16();
                                entry.BackgroundLayer.Height = br.ReadUInt16();
                                entry.BackgroundLayer.Colors.Red = br.ReadByte();
                                entry.BackgroundLayer.Colors.Green = br.ReadByte();
                                entry.BackgroundLayer.Colors.Blue = br.ReadByte();
                                entry.BackgroundLayer.Colors.Alpha = br.ReadByte();
                                entry.ForegroundLayer.X = br.ReadInt16();
                                entry.ForegroundLayer.Y = br.ReadInt16();
                                entry.ForegroundLayer.OuterLeftIconGroupLink = br.ReadByte();
                                entry.ForegroundLayer.OuterLeftIconSectionLink = br.ReadByte();
                                entry.ForegroundLayer.MiddleIconGroupLink = br.ReadByte();
                                entry.ForegroundLayer.MiddleIconSectionLink = br.ReadByte();
                                entry.ForegroundLayer.OuterRightIconGroupLink = br.ReadByte();
                                entry.ForegroundLayer.OuterRightIconSectionLink = br.ReadByte();
                                entry.ForegroundLayer.CustomClutLink = br.ReadUInt16();
                                entry.ForegroundLayer.Width = br.ReadUInt16();
                                entry.ForegroundLayer.Height = br.ReadUInt16();
                                entry.ForegroundLayer.Colors.Red = br.ReadByte();
                                entry.ForegroundLayer.Colors.Green = br.ReadByte();
                                entry.ForegroundLayer.Colors.Blue = br.ReadByte();
                                entry.ForegroundLayer.Colors.Alpha = br.ReadByte();
                                entry.AnimatedTexture.Scale = br.ReadSingle();
                                entry.AnimatedTexture.TextureFileLink = br.ReadUInt16();
                                entry.AnimatedTexture.IconGroupLink = br.ReadByte();
                                entry.AnimatedTexture.IconSectionLink = br.ReadByte();
                                entry.AnimatedTexture.CustomClutLink = br.ReadByte();
                                entry.AnimatedTexture.Y = br.ReadSByte();
                                entry.AnimatedTexture.Height = br.ReadByte();
                                entry.AnimatedTexture.Bloom = br.ReadByte();
                                break;
                            }
                        case 8:
                            {
                                entry = new EntryType8();
                                var entryFlags = br.ReadByte();
                                entry.IsVisible = (entryFlags >> 4 & 0x01) == 1;
                                entry.X = br.ReadInt16();
                                entry.Y = br.ReadInt16();
                                entry.Width = br.ReadUInt16();
                                entry.Height = br.ReadUInt16();
                                entry.GroupLink = br.ReadUInt16();
                                entry.Columns = br.ReadByte();
                                entry.Rows = br.ReadByte();
                                br.BaseStream.Seek(0x1C, SeekOrigin.Current);
                                break;
                            }
                        default:
                            throw new ArgumentException("Mrp: 'Entry -> Type' is unknown.");
                    }
                    group.Entries.Add($"Entry {j}", entry);
                }
                Groups.Add($"Group {i}", group);
                br.BaseStream.Position = preservedPosition;
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.BaseStream.Seek(0x30, SeekOrigin.Current); //set up header later

            var entryListOffsets = new List<long>();
            for (var i = 0; i < Groups.Count; i++)
            {
                var group = Groups.ElementAt(i).Value;
                var flags = group.IsVisible ? (byte)0x10 : (byte)0;

                bw.Write((byte)group.Entries.Count);
                bw.Write((ushort)(i * 4));
                bw.Write(flags);
                bw.Write(group.X);
                bw.Write(group.Y);
                bw.Write(group.Width);
                bw.Write(group.Height);
                bw.Write(group.Gap);
                entryListOffsets.Add(bw.BaseStream.Position);
                bw.BaseStream.Seek(0x04, SeekOrigin.Current);
            }

            for (var i = 0; i < Groups.Count; i++)
            {
                //set up entry list offset in group structure
                var currentPos = bw.BaseStream.Position;
                bw.BaseStream.Seek(entryListOffsets[i], SeekOrigin.Begin);
                var entryListOffset = currentPos - (entryListOffsets[i] - 0x10);
                bw.Write((uint)entryListOffset);
                bw.BaseStream.Seek(currentPos, SeekOrigin.Begin);

                var group = Groups.ElementAt(i).Value;
                for (var j = 0; j < group.Entries.Count; j++)
                {
                    var entry = group.Entries.ElementAt(j).Value;
                    var flags = entry.IsVisible ? (byte)0x10 : (byte)0;

                    bw.Write(entry.Size);
                    bw.Write(entry.Type);
                    bw.Write((byte)j);
                    bw.Write(flags);
                    bw.Write(entry.X);
                    bw.Write(entry.Y);
                    bw.Write(entry.Width);
                    bw.Write(entry.Height);

                    switch ((int)entry.Type)
                    {
                        case 1:
                            {
                                bw.Write(entry.GroupLink);
                                break;
                            }
                        case 2:
                            {
                                bw.Write(entry.TextureFileLink);
                                bw.Write(entry.IconGroupLink);
                                bw.Write(entry.IconSectionLink);
                                bw.Write(entry.IconEntryLink);
                                bw.Write(entry.CustomClutLink);
                                bw.Write(entry.ColorsTopLeft.Red);
                                bw.Write(entry.ColorsTopLeft.Green);
                                bw.Write(entry.ColorsTopLeft.Blue);
                                bw.Write(entry.ColorsTopLeft.Alpha);
                                bw.Write(entry.ColorsTopRight.Red);
                                bw.Write(entry.ColorsTopRight.Green);
                                bw.Write(entry.ColorsTopRight.Blue);
                                bw.Write(entry.ColorsTopRight.Alpha);
                                bw.Write(entry.ColorsBottomLeft.Red);
                                bw.Write(entry.ColorsBottomLeft.Green);
                                bw.Write(entry.ColorsBottomLeft.Blue);
                                bw.Write(entry.ColorsBottomLeft.Alpha);
                                bw.Write(entry.ColorsBottomRight.Red);
                                bw.Write(entry.ColorsBottomRight.Green);
                                bw.Write(entry.ColorsBottomRight.Blue);
                                bw.Write(entry.ColorsBottomRight.Alpha);
                                break;
                            }
                        case 4:
                            {
                                bw.Write(new byte[16]);
                                break;
                            }
                        case 5:
                            {
                                bw.Write(entry.Colors.Red);
                                bw.Write(entry.Colors.Green);
                                bw.Write(entry.Colors.Blue);
                                bw.Write(entry.Colors.Alpha);
                                bw.Write(entry.Scale);
                                bw.Write(entry.TextStyle);
                                bw.Write(entry.Unknown);
                                bw.Write(entry.TextLink);
                                break;
                            }
                        case 6:
                            {
                                bw.Write(new byte[12]);
                                break;
                            }
                        case 7:
                            {
                                var unknownFlag0 = entry.UnknownFlag0 ? (byte)0x02 : (byte)0;
                                bw.Write(entry.TextureFileLink);
                                bw.Write(entry.IconGroupLink);
                                bw.Write(entry.IconSectionLink);
                                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                                bw.Write(entry.Speed);
                                bw.Write(unknownFlag0);
                                bw.Write(entry.BackgroundLayer.X);
                                bw.Write(entry.BackgroundLayer.Y);
                                bw.Write(entry.BackgroundLayer.OuterLeftIconGroupLink);
                                bw.Write(entry.BackgroundLayer.OuterLeftIconSectionLink);
                                bw.Write(entry.BackgroundLayer.MiddleIconGroupLink);
                                bw.Write(entry.BackgroundLayer.MiddleIconSectionLink);
                                bw.Write(entry.BackgroundLayer.OuterRightIconGroupLink);
                                bw.Write(entry.BackgroundLayer.OuterRightIconSectionLink);
                                bw.Write(entry.BackgroundLayer.CustomClutLink);
                                bw.Write(entry.BackgroundLayer.Width);
                                bw.Write(entry.BackgroundLayer.Height);
                                bw.Write(entry.BackgroundLayer.Colors.Red);
                                bw.Write(entry.BackgroundLayer.Colors.Green);
                                bw.Write(entry.BackgroundLayer.Colors.Blue);
                                bw.Write(entry.BackgroundLayer.Colors.Alpha);
                                bw.Write(entry.ForegroundLayer.X);
                                bw.Write(entry.ForegroundLayer.Y);
                                bw.Write(entry.ForegroundLayer.OuterLeftIconGroupLink);
                                bw.Write(entry.ForegroundLayer.OuterLeftIconSectionLink);
                                bw.Write(entry.ForegroundLayer.MiddleIconGroupLink);
                                bw.Write(entry.ForegroundLayer.MiddleIconSectionLink);
                                bw.Write(entry.ForegroundLayer.OuterRightIconGroupLink);
                                bw.Write(entry.ForegroundLayer.OuterRightIconSectionLink);
                                bw.Write(entry.ForegroundLayer.CustomClutLink);
                                bw.Write(entry.ForegroundLayer.Width);
                                bw.Write(entry.ForegroundLayer.Height);
                                bw.Write(entry.ForegroundLayer.Colors.Red);
                                bw.Write(entry.ForegroundLayer.Colors.Green);
                                bw.Write(entry.ForegroundLayer.Colors.Blue);
                                bw.Write(entry.ForegroundLayer.Colors.Alpha);
                                bw.Write(entry.AnimatedTexture.Scale);
                                bw.Write(entry.AnimatedTexture.TextureFileLink);
                                bw.Write(entry.AnimatedTexture.IconGroupLink);
                                bw.Write(entry.AnimatedTexture.IconSectionLink);
                                bw.Write(entry.AnimatedTexture.CustomClutLink);
                                bw.Write(entry.AnimatedTexture.Y);
                                bw.Write(entry.AnimatedTexture.Height);
                                bw.Write(entry.AnimatedTexture.Bloom);
                                break;
                            }
                        case 8:
                            {
                                bw.Write(entry.GroupLink);
                                bw.Write(entry.Columns);
                                bw.Write(entry.Rows);
                                bw.Write(new byte[28]);
                                break;
                            }
                        default:
                            {
                                throw new ArgumentException("Mrp: 'Entry -> Type' is unknown.");
                            }
                    }
                }
            }
            BinaryHelper.Align(bw, 16);

            var textureListOffset = (uint)bw.BaseStream.Position;
            foreach (var texName in Textures.Values.Select(i => BinaryHelper.GetBytesByEncodedString(i.Name)))
            {
                bw.Write(texName);
                bw.Write(new byte[14 - texName.Length + 0x02]); //skip reserved characters, state and link
            }
            var fileSizeOffset = (uint)bw.BaseStream.Position;

            //set up header
            bw.BaseStream.Seek(0x00, SeekOrigin.Begin);
            bw.Write(magic);
            bw.BaseStream.Seek(0x01, SeekOrigin.Current); //skip state
            bw.Write(Textures.Count);
            bw.Write(textureListOffset);
            bw.BaseStream.Seek(0x04, SeekOrigin.Current);
            bw.Write(fileSizeOffset);
            bw.BaseStream.Seek(0x04, SeekOrigin.Current);
            bw.Write(fileSizeOffset);
            bw.Write(Groups.Count);
            bw.Write(0x30);
            bw.BaseStream.Seek(0x04, SeekOrigin.Current);
            bw.Write(fileSizeOffset);
        }

        public class Group
        {
            [JsonPropertyName("X")]
            public short X { get; set; }

            [JsonPropertyName("Y")]
            public short Y { get; set; }

            [JsonPropertyName("Width")]
            public ushort Width { get; set; }

            [JsonPropertyName("Height")]
            public ushort Height { get; set; }

            [JsonPropertyName("Gap")]
            public int Gap { get; set; }

            [JsonPropertyName("Is Visible")]
            public bool IsVisible { get; set; }

            [JsonConverter(typeof(EntriesConverter))]
            [JsonPropertyName("Entries")]
            public Dictionary<string, dynamic> Entries { get; set; }

            public Group()
            {
                Entries = new Dictionary<string, dynamic>();
            }

            [JsonConstructor]
            public Group(Dictionary<string, dynamic> entries)
            {
                Entries = entries;
            }
        }

        public class Entry
        {
            [JsonPropertyName("Type")]
            [JsonPropertyOrder(-6)]
            public byte Type { get; set; }

            [JsonPropertyName("X")]
            [JsonPropertyOrder(-5)]
            public short X { get; set; }

            [JsonPropertyName("Y")]
            [JsonPropertyOrder(-4)]
            public short Y { get; set; }

            [JsonPropertyName("Width")]
            [JsonPropertyOrder(-3)]
            public ushort Width { get; set; }

            [JsonPropertyName("Height")]
            [JsonPropertyOrder(-2)]
            public ushort Height { get; set; }

            [JsonPropertyName("Is Visible")]
            [JsonPropertyOrder(-1)]
            public bool IsVisible { get; set; }
        }

        public class EntryType1 : Entry
        {
            public readonly byte Size = 0x10;

            [JsonPropertyName("Group Link")]
            [JsonPropertyOrder(0)]
            public uint GroupLink { get; set; }

            public EntryType1()
            {
                Type = 1;
            }
        }

        public class EntryType2 : Entry
        {
            public readonly byte Size = 0x24;

            [JsonPropertyName("Texture File Link")]
            [JsonPropertyOrder(0)]
            public ushort TextureFileLink { get; set; }

            [JsonPropertyName("Icon Section Link")]
            [JsonPropertyOrder(1)]
            public byte IconSectionLink { get; set; }

            [JsonPropertyName("Icon Group Link")]
            [JsonPropertyOrder(2)]
            public byte IconGroupLink { get; set; }

            [JsonPropertyName("Icon Entry Link")]
            [JsonPropertyOrder(3)]
            public ushort IconEntryLink { get; set; }

            [JsonPropertyName("Custom Clut Link")]
            [JsonPropertyOrder(4)]
            public ushort CustomClutLink { get; set; }

            [JsonPropertyName("Colors (Top Left)")]
            [JsonPropertyOrder(5)]
            public Rgba ColorsTopLeft { get; set; }

            [JsonPropertyName("Colors (Top Right)")]
            [JsonPropertyOrder(6)]
            public Rgba ColorsTopRight { get; set; }

            [JsonPropertyName("Colors (Bottom Left)")]
            [JsonPropertyOrder(7)]
            public Rgba ColorsBottomLeft { get; set; }

            [JsonPropertyName("Colors (Bottom Right)")]
            [JsonPropertyOrder(8)]
            public Rgba ColorsBottomRight { get; set; }

            public EntryType2()
            {
                Type = 2;
                ColorsTopLeft = new Rgba();
                ColorsTopRight = new Rgba();
                ColorsBottomLeft = new Rgba();
                ColorsBottomRight = new Rgba();
            }
        }

        public class EntryType4 : Entry
        {
            public readonly byte Size = 0x1C;

            public EntryType4()
            {
                Type = 4;
            }
        }

        public class EntryType5 : Entry
        {
            public readonly byte Size = 0x18;

            [JsonPropertyName("Text Link")]
            [JsonPropertyOrder(0)]
            public int TextLink { get; set; }

            [JsonPropertyName("Text Style")]
            [JsonPropertyOrder(1)]
            public byte TextStyle { get; set; }

            [JsonPropertyName("Scale")]
            [JsonPropertyOrder(2)]
            public byte Scale { get; set; }

            [JsonPropertyName("Unknown")]
            [JsonPropertyOrder(3)]
            public ushort Unknown { get; set; }

            [JsonPropertyName("Colors")]
            [JsonPropertyOrder(4)]
            public Rgba Colors { get; set; }

            public EntryType5()
            {
                Type = 5;
                Colors = new Rgba();
            }
        }

        public class EntryType6 : Entry
        {
            public readonly byte Size = 0x18;

            public EntryType6()
            {
                Type = 6;
            }
        }

        public class EntryType7 : Entry
        {
            public readonly byte Size = 0x48;

            [JsonPropertyName("Texture File Link")]
            [JsonPropertyOrder(0)]
            public ushort TextureFileLink { get; set; }

            [JsonPropertyName("Icon Section Link")]
            [JsonPropertyOrder(1)]
            public byte IconSectionLink { get; set; }

            [JsonPropertyName("Icon Group Link")]
            [JsonPropertyOrder(2)]
            public byte IconGroupLink { get; set; }

            [JsonPropertyName("Speed")]
            [JsonPropertyOrder(3)]
            public byte Speed { get; set; }

            [JsonPropertyName("Unknown Flag 0")]
            [JsonPropertyOrder(4)]
            public bool UnknownFlag0 { get; set; }

            [JsonPropertyName("Background Layer")]
            [JsonPropertyOrder(5)]
            public Layer BackgroundLayer { get; set; }

            [JsonPropertyName("Foreground Layer")]
            [JsonPropertyOrder(6)]
            public Layer ForegroundLayer { get; set; }

            [JsonPropertyName("Animated Texture")]
            [JsonPropertyOrder(7)]
            public AnimTex AnimatedTexture { get; set; }

            public EntryType7()
            {
                Type = 7;
                BackgroundLayer = new Layer();
                ForegroundLayer = new Layer();
                AnimatedTexture = new AnimTex();
            }
        }

        public class EntryType8 : Entry
        {
            public readonly byte Size = 0x2C;

            [JsonPropertyName("Group Link")]
            [JsonPropertyOrder(0)]
            public ushort GroupLink { get; set; }

            [JsonPropertyName("Columns")]
            [JsonPropertyOrder(1)]
            public byte Columns { get; set; }

            [JsonPropertyName("Rows")]
            [JsonPropertyOrder(2)]
            public byte Rows { get; set; }

            public EntryType8()
            {
                Type = 8;
            }
        }

        public class Layer
        {
            [JsonPropertyName("Outer Left Icon Section Link")]
            public byte OuterLeftIconSectionLink { get; set; }

            [JsonPropertyName("Outer Left Icon Group Link")]
            public byte OuterLeftIconGroupLink { get; set; }

            [JsonPropertyName("Middle Icon Section Link")]
            public byte MiddleIconSectionLink { get; set; }

            [JsonPropertyName("Middle Icon Group Link")]
            public byte MiddleIconGroupLink { get; set; }

            [JsonPropertyName("Outer Right Icon Section Link")]
            public byte OuterRightIconSectionLink { get; set; }

            [JsonPropertyName("Outer Right Icon Group Link")]
            public byte OuterRightIconGroupLink { get; set; }

            [JsonPropertyName("Custom Clut Link")]
            public ushort CustomClutLink { get; set; }

            [JsonPropertyName("X")]
            public short X { get; set; }

            [JsonPropertyName("Y")]
            public short Y { get; set; }

            [JsonPropertyName("Width")]
            public ushort Width { get; set; }

            [JsonPropertyName("Height")]
            public ushort Height { get; set; }

            [JsonPropertyName("Colors")]
            public Rgba Colors { get; set; }

            public Layer()
            {
                Colors = new Rgba();
            }
        }

        public class AnimTex
        {
            [JsonPropertyName("Texture File Link")]
            public ushort TextureFileLink { get; set; }

            [JsonPropertyName("Icon Section Link")]
            public byte IconSectionLink { get; set; }

            [JsonPropertyName("Icon Group Link")]
            public byte IconGroupLink { get; set; }

            [JsonPropertyName("Custom Clut Link")]
            public byte CustomClutLink { get; set; }

            [JsonPropertyName("Scale")]
            public float Scale { get; set; }

            [JsonPropertyName("Y")]
            public sbyte Y { get; set; }

            [JsonPropertyName("Height")]
            public byte Height { get; set; }

            [JsonPropertyName("Bloom")]
            public byte Bloom { get; set; }
        }

        public class Texture
        {
            [JsonPropertyName("Name")]
            public string Name { get; set; }
        }

        private class EntriesConverter : JsonConverter<Dictionary<string, dynamic>>
        {
            public override Dictionary<string, dynamic> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var entries = new Dictionary<string, dynamic>();
                var jsonDoc = JsonDocument.ParseValue(ref reader);

                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    var typeName = property.Value.GetProperty("Type").GetByte();
                    var typeClass = entryTypeMap.GetValueOrDefault(typeName);
                    if (typeClass == null)
                    {
                        throw new ArgumentException("Mrp: 'Entry -> Type' is unknown.");
                    }

                    var data = JsonSerializer.Deserialize(property.Value.ToString(), typeClass, options);
                    entries.Add(property.Name, data);
                }
                return entries;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, dynamic> data, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, data, options);
            }

            private readonly Dictionary<byte, Type> entryTypeMap = new()
            {
                { 1, typeof(EntryType1) },
                { 2, typeof(EntryType2) },
                { 4, typeof(EntryType4) },
                { 5, typeof(EntryType5) },
                { 6, typeof(EntryType6) },
                { 7, typeof(EntryType7) },
                { 8, typeof(EntryType8) }
            };
        }
    }
}