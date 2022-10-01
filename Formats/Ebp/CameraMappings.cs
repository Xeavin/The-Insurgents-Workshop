using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Ebp
{
    public class CameraMappings
    {
        public static readonly byte[] Magic = { 0x43, 0x4D, 0x4C, 0x31 }; //CML1

        [JsonPropertyName("Camera Mappings")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public CameraMappings(Dictionary<string, Entry> entries)
        {
            Entries = entries;
        }

        public CameraMappings(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(4).SequenceEqual(Magic))
            {
                throw new ArgumentException("Ebp Section 8: Unexpected magic.");
            }

            var entryCount = br.ReadUInt32();
            br.BaseStream.Seek(0x10, SeekOrigin.Begin); //skip header
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = new Entry();
                var labelOffset = br.ReadUInt32();
                entry.Link = br.ReadUInt32();

                var oldPosition = br.BaseStream.Position;
                br.BaseStream.Position = labelOffset;

                var labelBytes = new List<byte>();
                while (true)
                {
                    var charAsByte = br.ReadByte();
                    if (charAsByte == 0)
                    {
                        break;
                    }
                    labelBytes.Add(charAsByte);
                }

                entry.Label = BinaryHelper.GetEncodedStringByBytes(labelBytes.ToArray());
                br.BaseStream.Position = oldPosition + 0x08;
                Entries.Add($"Camera Mapping {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(Magic);
            bw.Write((uint)Entries.Count);

            //reserve space for label offsets and links
            bw.BaseStream.Seek(0x10 + Entries.Count * 0x10, SeekOrigin.Begin); //0x10 for header, x*0x10 for x entries.

            var entryLabelOffsetList = new List<uint>();
            foreach (var entry in Entries.Values)
            {
                entryLabelOffsetList.Add((uint)bw.BaseStream.Position);
                bw.Write(BinaryHelper.GetBytesByEncodedString(entry.Label));
                bw.Write((byte)0x00); //mark string end
            }

            //write entry label offsets and links
            bw.BaseStream.Seek(0x10, SeekOrigin.Begin);
            for (var i = 0; i < entryLabelOffsetList.Count; i++)
            {
                bw.Write(entryLabelOffsetList[i]);
                bw.Write(Entries.ElementAt(i).Value.Link);
                bw.BaseStream.Seek(0x08, SeekOrigin.Current); //skip unused 8 bytes
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Label")]
            public string Label { get; set; }

            [JsonPropertyName("Link")]
            public uint Link { get; set; }
        }
    }
}
