using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Movies : St2e
    {
        [JsonPropertyName("Movies")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Movies(Dictionary<string, Entry> entries)
        {
            Entries = entries;
            SetupHeader((uint)entries.Count, 0x08);
        }

        public Movies(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                br.BaseStream.Seek(0x01, SeekOrigin.Current);
                entry.FadeInFrameTimeStart = br.ReadByte();
                entry.FadeInFrameTimeDuration = br.ReadByte();
                entry.FadeOutFrameTimeStart = br.ReadByte();
                entry.FadeOutFrameTimeDuration = br.ReadByte();
                var flags = br.ReadByte();
                entry.FadeInAndFadeOutColor = (byte)(flags & 0x03);
                br.BaseStream.Seek(0x02, SeekOrigin.Current);
                Entries.Add($"Movie {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                byte flags = 0;
                flags |= entry.FadeInAndFadeOutColor;

                bw.BaseStream.Seek(0x01, SeekOrigin.Current);
                bw.Write(entry.FadeInFrameTimeStart);
                bw.Write(entry.FadeInFrameTimeDuration);
                bw.Write(entry.FadeOutFrameTimeStart);
                bw.Write(entry.FadeOutFrameTimeDuration);
                bw.BaseStream.Seek(0x02, SeekOrigin.Current);
                bw.Write(flags);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Fade-in Frame Time Start")]
            public byte FadeInFrameTimeStart { get; set; }

            [JsonPropertyName("Fade-in Frame Time Duration")]
            public byte FadeInFrameTimeDuration { get; set; }

            [JsonPropertyName("Fade-out Frame Time Start (Backwards)")]
            public byte FadeOutFrameTimeStart { get; set; }

            [JsonPropertyName("Fade-out Frame Time Duration (Backwards)")]
            public byte FadeOutFrameTimeDuration { get; set; }

            private byte fadeInAndFadeOutColor;

            [JsonPropertyName("Fade-in And Fade-out Color")]
            public byte FadeInAndFadeOutColor
            {
                get => fadeInAndFadeOutColor;
                set
                {
                    if (value > 3)
                    {
                        throw new ArgumentException("Battlepack Section 69: 'Fade-in And Fade-out Color' cannot be higher than 3.");
                    }
                    fadeInAndFadeOutColor = value;
                }
            }
        }
    }
}