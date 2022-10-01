using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Formats.Ebp
{
    public class Cameras
    {
        private readonly byte[] magic = { 0x43, 0x41, 0x4D, 0x37 }; //CAM7

        [JsonPropertyName("First Info List")]
        public Dictionary<string, FirstInfo> FirstInfoDic { get; set; }

        [JsonPropertyName("Second Info List")]
        public Dictionary<string, SecondInfo> SecondInfoDic { get; set; }

        [JsonPropertyName("Third Info List")]
        public Dictionary<string, ThirdInfo> ThirdInfoDic { get; set; }

        [JsonConstructor]
        public Cameras(Dictionary<string, FirstInfo> firstInfoDic, Dictionary<string, SecondInfo> secondInfoDic, Dictionary<string, ThirdInfo> thirdInfoDic)
        {
            if (firstInfoDic.Values.Any(i => i.Entries.Values.Any(j => j.Type == 2 && j.Unknown6.Length != 13)))
            {
                throw new ArgumentException("Ebp Section 9: 'First Info -> Unknown 6' must contain exactly 13 entries.");
            }

            if (firstInfoDic.Values.Any(i => i.Entries.Values.Any(j => j.Type == 2 && j.Unknown7.Length != 2)))
            {
                throw new ArgumentException("Ebp Section 9: 'First Info -> Unknown 7' must contain exactly 2 entries.");
            }

            if (firstInfoDic.Values.Any(i => i.Entries.Values.Any(j => j.Type == 2 && j.Unused2.Length != 6)))
            {
                throw new ArgumentException("Ebp Section 9: 'First Info -> Unused 2' must contain exactly 6 entries.");
            }

            FirstInfoDic = firstInfoDic;
            SecondInfoDic = secondInfoDic;
            ThirdInfoDic = thirdInfoDic;
        }

        public Cameras(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(4).SequenceEqual(magic))
            {
                throw new ArgumentException("Ebp Section 9: Unexpected magic.");
            }

            var firstInfoCount = br.ReadUInt32();
            var secondInfoCount = br.ReadUInt32();
            var thirdInfoCount = br.ReadUInt32();
            br.BaseStream.Seek(0x1C, SeekOrigin.Begin);
            var entryListOffset = br.ReadUInt32();
            br.BaseStream.Seek(entryListOffset, SeekOrigin.Begin);

            FirstInfoDic = new Dictionary<string, FirstInfo>();
            for (var i = 0; i < firstInfoCount; i++)
            {
                br.BaseStream.Seek(0x04, SeekOrigin.Current); //skip unused 4 bytes
                var size = br.ReadInt32();

                var firstInfo = new FirstInfo();
                var labelBytes = br.ReadBytes(24).Where(j => j != 0).ToArray();
                firstInfo.Label = BinaryHelper.GetEncodedStringByBytes(labelBytes);
                firstInfo.Flags = br.ReadUInt32();
                firstInfo.Unknown0 = br.ReadUInt16();
                firstInfo.Unknown1 = br.ReadUInt16();

                var remSize = size - 0x28;
                var count = 0;
                while (remSize > 4)
                {
                    var type = br.ReadByte();
                    br.BaseStream.Seek(0x03, SeekOrigin.Current); //skip unused 3 bytes
                    switch ((int)type)
                    {
                        case 1:
                            var type1 = new FirstInfo.Type1();
                            type1.Camera.X = br.ReadSingle();
                            type1.Camera.Y = br.ReadSingle();
                            type1.Camera.Z = br.ReadSingle();
                            type1.Camera.W = br.ReadSingle();

                            type1.ViewAngle.X = br.ReadSingle();
                            type1.ViewAngle.Y = br.ReadSingle();
                            type1.ViewAngle.Z = br.ReadSingle();
                            type1.ViewAngle.W = br.ReadSingle();

                            type1.Roll = br.ReadSingle();
                            type1.VerticalFov = br.ReadSingle();
                            type1.Unknown3 = br.ReadSingle();
                            type1.Unknown4 = br.ReadSingle();
                            type1.Unused0 = br.ReadUInt32();
                            type1.Unknown5 = br.ReadSingle();
                            type1.Unused1 = br.ReadUInt32();

                            firstInfo.Entries.Add($"Entry {count}", type1);
                            remSize -= type1.Size;
                            break;
                        case 2:
                            var type2 = new FirstInfo.Type2();

                            for (var j = 0; j < 13; j++)
                            {
                                type2.Unknown6[j] = br.ReadUInt16();
                            }

                            for (var j = 0; j < 2; j++)
                            {
                                type2.Unknown7[j] = br.ReadUInt16();
                            }

                            type2.Unknown8 = br.ReadUInt16();
                            type2.Unknown9 = br.ReadByte();
                            type2.Unknown10 = br.ReadByte();
                            type2.Unused2 = br.ReadBytes(6);

                            firstInfo.Entries.Add($"Entry {count}", type2);
                            remSize -= type2.Size;
                            break;
                        default:
                            throw new ArgumentException("Ebp Section 9: 'First Info -> Type' is unknown.");
                    }

                    count++;
                }

                firstInfo.Unknown2 = br.ReadUInt32();
                FirstInfoDic.Add($"First Info {i}", firstInfo);
            }

            SecondInfoDic = new Dictionary<string, SecondInfo>();
            for (var i = 0; i < secondInfoCount; i++)
            {
                var extendedInfo = new SecondInfo();

                br.BaseStream.Seek(0x04, SeekOrigin.Current); //skip size property
                var entryCount = br.ReadUInt32();
                extendedInfo.Unknown11 = br.ReadSingle();
                extendedInfo.Unknown12 = br.ReadSingle();
                extendedInfo.Unknown13 = br.ReadSingle();
                extendedInfo.Unknown14 = br.ReadSingle();

                for (var j = 0; j < entryCount; j++)
                {
                    var entry = new SecondInfo.Entry();
                    entry.Unknown15 = br.ReadSingle();
                    entry.Unknown16 = br.ReadSingle();
                    entry.Unknown17 = br.ReadSingle();
                    entry.Unknown18 = br.ReadSingle();
                    entry.Unknown19 = br.ReadSingle();
                    entry.Unknown20 = br.ReadSingle();
                    entry.Unknown21 = br.ReadSingle();
                    entry.Unknown22 = br.ReadSingle();
                    entry.Unknown23 = br.ReadSingle();
                    extendedInfo.Entries.Add($"Entry {j}", entry);
                }

                SecondInfoDic.Add($"Second Info {i}", extendedInfo);
            }

            ThirdInfoDic = new Dictionary<string, ThirdInfo>();
            for (var i = 0; i < thirdInfoCount; i++)
            {
                br.BaseStream.Seek(0x04, SeekOrigin.Current); //skip size property
                var firstEntryCount = br.ReadUInt16();
                var secondEntryCount = br.ReadUInt16();

                var thirdInfo = new ThirdInfo();
                thirdInfo.Unknown24.X = br.ReadSingle();
                thirdInfo.Unknown24.Y = br.ReadSingle();
                thirdInfo.Unknown24.Z = br.ReadSingle();

                thirdInfo.Unknown25.X = br.ReadSingle();
                thirdInfo.Unknown25.Y = br.ReadSingle();
                thirdInfo.Unknown25.Z = br.ReadSingle();

                for (var j = 0; j < firstEntryCount; j++)
                {
                    var firstEntry = new ThirdInfo.FirstEntry();
                    firstEntry.Unknown26.X = br.ReadSingle();
                    firstEntry.Unknown26.Y = br.ReadSingle();
                    firstEntry.Unknown26.Z = br.ReadSingle();
                    firstEntry.Unknown26.W = br.ReadSingle();

                    firstEntry.Unknown27.X = br.ReadSingle();
                    firstEntry.Unknown27.Y = br.ReadSingle();
                    firstEntry.Unknown27.Z = br.ReadSingle();
                    firstEntry.Unknown27.W = br.ReadSingle();

                    firstEntry.Unknown28.X = br.ReadSingle();
                    firstEntry.Unknown28.Y = br.ReadSingle();
                    firstEntry.Unknown28.Z = br.ReadSingle();
                    firstEntry.Unknown28.W = br.ReadSingle();

                    firstEntry.Unknown29 = br.ReadSingle();
                    firstEntry.Unused3 = br.ReadSingle();
                    thirdInfo.FirstEntries.Add($"First Entry {j}", firstEntry);
                }

                for (var j = 0; j < secondEntryCount; j++)
                {
                    var secondEntry = new ThirdInfo.SecondEntry();
                    secondEntry.Unknown30 = br.ReadSingle();
                    secondEntry.Unknown31 = br.ReadSingle();

                    thirdInfo.SecondEntries.Add($"Second Entry {j}", secondEntry);
                }

                ThirdInfoDic.Add($"Third Info {i}", thirdInfo);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(magic);
            bw.Write((uint)FirstInfoDic.Count);
            bw.Write((uint)SecondInfoDic.Count);
            bw.Write((uint)ThirdInfoDic.Count);

            bw.BaseStream.Seek(0x1C, SeekOrigin.Begin);
            bw.Write(0x00000020); //first info list offset

            foreach (var firstInfo in FirstInfoDic.Values)
            {
                var size = 0x2C; //header(0x28) + footer(0x04) size
                bw.BaseStream.Seek(0x04, SeekOrigin.Current); //skip unused 4 bytes

                //preserve position to set size later
                var preservedPosition = bw.BaseStream.Position;
                bw.BaseStream.Seek(0x04, SeekOrigin.Current);

                var labelBytes = BinaryHelper.GetBytesByEncodedString(firstInfo.Label);
                bw.Write(labelBytes);
                var labelPadding = 0x18 - labelBytes.Length;
                if (labelPadding > 0)
                {
                    bw.Write(new byte[labelPadding]);
                }

                bw.Write(firstInfo.Flags);
                bw.Write(firstInfo.Unknown0);
                bw.Write(firstInfo.Unknown1);

                foreach (var entry in firstInfo.Entries.Values)
                {
                    bw.Write(entry.Type);
                    bw.BaseStream.Seek(0x03, SeekOrigin.Current); //skip unused 3 bytes

                    switch ((int)entry.Type)
                    {
                        case 1:
                            bw.Write(entry.Camera.X);
                            bw.Write(entry.Camera.Y);
                            bw.Write(entry.Camera.Z);
                            bw.Write(entry.Camera.W);
                            bw.Write(entry.ViewAngle.X);
                            bw.Write(entry.ViewAngle.Y);
                            bw.Write(entry.ViewAngle.Z);
                            bw.Write(entry.ViewAngle.W);
                            bw.Write(entry.Roll);
                            bw.Write(entry.Fov);
                            bw.Write(entry.Unknown3);
                            bw.Write(entry.Unknown4);
                            bw.Write(entry.Unused0);
                            bw.Write(entry.Unknown5);
                            bw.Write(entry.Unused1);

                            size += entry.Size;
                            break;
                        case 2:
                            for (var j = 0; j < 13; j++)
                            {
                                bw.Write(entry.Unknown6[j]);
                            }

                            for (var j = 0; j < 2; j++)
                            {
                                bw.Write(entry.Unknown7[j]);
                            }

                            bw.Write(entry.Unknown8);
                            bw.Write(entry.Unknown9);
                            bw.Write(entry.Unknown10);
                            bw.Write(entry.Unused2);

                            size += entry.Size;
                            break;
                        default:
                            throw new ArgumentException("Ebp Section 9: 'First Info -> Type' is unknown.");
                    }
                }
                bw.Write(firstInfo.Unknown2);

                //write entry size
                var curPos = bw.BaseStream.Position;
                bw.BaseStream.Position = preservedPosition;
                bw.Write(size);
                bw.BaseStream.Position = curPos;
            }

            foreach (var secondInfo in SecondInfoDic.Values)
            {
                var entryCount = secondInfo.Entries.Count;
                var size = 0x18 + entryCount * 0x24;
                bw.Write(size);
                bw.Write(entryCount);

                bw.Write(secondInfo.Unknown11);
                bw.Write(secondInfo.Unknown12);
                bw.Write(secondInfo.Unknown13);
                bw.Write(secondInfo.Unknown14);

                foreach (var entry in secondInfo.Entries.Values)
                {
                    bw.Write(entry.Unknown15);
                    bw.Write(entry.Unknown16);
                    bw.Write(entry.Unknown17);
                    bw.Write(entry.Unknown18);
                    bw.Write(entry.Unknown19);
                    bw.Write(entry.Unknown20);
                    bw.Write(entry.Unknown21);
                    bw.Write(entry.Unknown22);
                    bw.Write(entry.Unknown23);
                }
            }

            foreach (var thirdInfo in ThirdInfoDic.Values)
            {
                var firstEntryCount = thirdInfo.FirstEntries.Count;
                var secondEntryCount = thirdInfo.SecondEntries.Count;
                var size = 0x20 + firstEntryCount * 0x38 + secondEntryCount * 0x08; //0x20 for header, x*0x38 for x first entries, x*0x08 for x second entries.
                bw.Write(size);
                bw.Write((ushort)firstEntryCount);
                bw.Write((ushort)secondEntryCount);

                bw.Write(thirdInfo.Unknown24.X);
                bw.Write(thirdInfo.Unknown24.Y);
                bw.Write(thirdInfo.Unknown24.Z);

                bw.Write(thirdInfo.Unknown25.X);
                bw.Write(thirdInfo.Unknown25.Y);
                bw.Write(thirdInfo.Unknown25.Z);

                foreach (var firstEntry in thirdInfo.FirstEntries.Values)
                {
                    bw.Write(firstEntry.Unknown26.X);
                    bw.Write(firstEntry.Unknown26.Y);
                    bw.Write(firstEntry.Unknown26.Z);
                    bw.Write(firstEntry.Unknown26.W);

                    bw.Write(firstEntry.Unknown27.X);
                    bw.Write(firstEntry.Unknown27.Y);
                    bw.Write(firstEntry.Unknown27.Z);
                    bw.Write(firstEntry.Unknown27.W);

                    bw.Write(firstEntry.Unknown28.X);
                    bw.Write(firstEntry.Unknown28.Y);
                    bw.Write(firstEntry.Unknown28.Z);
                    bw.Write(firstEntry.Unknown28.W);

                    bw.Write(firstEntry.Unknown29);
                    bw.Write(firstEntry.Unused3);
                }

                foreach (var secondEntry in thirdInfo.SecondEntries.Values)
                {
                    bw.Write(secondEntry.Unknown30);
                    bw.Write(secondEntry.Unknown31);
                }
            }
            BinaryHelper.Align(bw, 16);
        }

        public class FirstInfo
        {
            [JsonPropertyName("Label")]
            public string Label { get; set; }

            [JsonPropertyName("Flags")]
            public uint Flags { get; set; }

            [JsonPropertyName("Unknown 0")]
            public ushort Unknown0 { get; set; }

            [JsonPropertyName("Unknown 1")]
            public ushort Unknown1 { get; set; }

            [JsonPropertyName("Unknown 2")]
            public uint Unknown2 { get; set; }

            [JsonConverter(typeof(EntriesConverter))]
            [JsonPropertyName("Entries")]
            public Dictionary<string, dynamic> Entries { get; set; }

            public FirstInfo()
            {
                Entries = new Dictionary<string, dynamic>();
            }

            public class Type1
            {
                public readonly byte Size = 0x40;

                [JsonPropertyName("Type")]
                public byte Type { get; set; }

                [JsonPropertyName("Camera")]
                public Quaternion Camera { get; set; }

                [JsonPropertyName("View Angle")]
                public Quaternion ViewAngle { get; set; }

                [JsonPropertyName("Roll (Degrees)")]
                public float Roll { get; set; }

                [JsonPropertyName("Vertical Fov (Degrees)")]
                public float VerticalFov { get; set; }

                [JsonPropertyName("Unknown 3")]
                public float Unknown3 { get; set; }

                [JsonPropertyName("Unknown 4")]
                public float Unknown4 { get; set; }

                [JsonPropertyName("Unknown 5")]
                public float Unknown5 { get; set; }

                [JsonPropertyName("Unused 0?")]
                public uint Unused0 { get; set; }

                [JsonPropertyName("Unused 1?")]
                public uint Unused1 { get; set; }

                public Type1()
                {
                    Type = 1;
                    Camera = new Quaternion();
                    ViewAngle = new Quaternion();
                }
            }

            public class Type2
            {
                public readonly byte Size = 0x2C;

                [JsonPropertyName("Type")]
                public byte Type { get; set; }

                [JsonPropertyName("Unknown 6")]
                public ushort[] Unknown6 { get; set; } //max count: 13

                [JsonPropertyName("Unknown 7")]
                public ushort[] Unknown7 { get; set; } //max count: 2

                [JsonPropertyName("Unknown 8")]
                public ushort Unknown8 { get; set; }

                [JsonPropertyName("Unknown 9")]
                public byte Unknown9 { get; set; }

                [JsonPropertyName("Unknown 10")]
                public byte Unknown10 { get; set; }

                [JsonPropertyName("Unused 2?")]
                public byte[] Unused2 { get; set; } //max count: 6

                public Type2()
                {
                    Type = 2;
                    Unknown6 = new ushort[13];
                    Unknown7 = new ushort[2];
                    Unused2 = new byte[6];
                }
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
                            throw new ArgumentException("Ebp Section 9: 'First Info -> Type' is unknown.");
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
                    { 1, typeof(Type1) },
                    { 2, typeof(Type2) }
                };
            }
        }

        public class SecondInfo
        {
            [JsonPropertyName("Unknown 11")]
            public float Unknown11 { get; set; }

            [JsonPropertyName("Unknown 12")]
            public float Unknown12 { get; set; }

            [JsonPropertyName("Unknown 13")]
            public float Unknown13 { get; set; }

            [JsonPropertyName("Unknown 14")]
            public float Unknown14 { get; set; }

            [JsonPropertyName("Entries")]
            public Dictionary<string, Entry> Entries { get; set; }

            public SecondInfo()
            {
                Entries = new Dictionary<string, Entry>();
            }

            public class Entry
            {
                [JsonPropertyName("Unknown 15")]
                public float Unknown15 { get; set; }

                [JsonPropertyName("Unknown 16")]
                public float Unknown16 { get; set; }

                [JsonPropertyName("Unknown 17")]
                public float Unknown17 { get; set; }

                [JsonPropertyName("Unknown 18")]
                public float Unknown18 { get; set; }

                [JsonPropertyName("Unknown 19")]
                public float Unknown19 { get; set; }

                [JsonPropertyName("Unknown 20")]
                public float Unknown20 { get; set; }

                [JsonPropertyName("Unknown 21")]
                public float Unknown21 { get; set; }

                [JsonPropertyName("Unknown 22")]
                public float Unknown22 { get; set; }

                [JsonPropertyName("Unknown 23")]
                public float Unknown23 { get; set; }
            }
        }

        public class ThirdInfo
        {
            [JsonPropertyName("Unknown 24")]
            public Vector Unknown24 { get; set; }

            [JsonPropertyName("Unknown 25")]
            public Vector Unknown25 { get; set; }

            [JsonPropertyName("First Entries")]
            public Dictionary<string, FirstEntry> FirstEntries { get; set; }

            [JsonPropertyName("Second Entries")]
            public Dictionary<string, SecondEntry> SecondEntries { get; set; }

            public ThirdInfo()
            {
                Unknown24 = new Vector();
                Unknown25 = new Vector();
                FirstEntries = new Dictionary<string, FirstEntry>();
                SecondEntries = new Dictionary<string, SecondEntry>();
            }

            public class FirstEntry
            {
                [JsonPropertyName("Unknown 26")]
                public Quaternion Unknown26 { get; set; }

                [JsonPropertyName("Unknown 27")]
                public Quaternion Unknown27 { get; set; }

                [JsonPropertyName("Unknown 28")]
                public Quaternion Unknown28 { get; set; }

                [JsonPropertyName("Unused 29")]
                public float Unknown29 { get; set; }

                [JsonPropertyName("Unused 3?")]
                public float Unused3 { get; set; }

                public FirstEntry()
                {
                    Unknown26 = new Quaternion();
                    Unknown27 = new Quaternion();
                    Unknown28 = new Quaternion();
                }
            }

            public class SecondEntry
            {
                [JsonPropertyName("Unknown 30")]
                public float Unknown30 { get; set; }

                [JsonPropertyName("Unknown 31")]
                public float Unknown31 { get; set; }
            }
        }
    }
}
