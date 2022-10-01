using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class Rewards : St2e
    {
        [JsonPropertyName("Rewards")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public Rewards(Dictionary<string, Entry> entries)
        {
            if (entries.Values.Any(i => i.PreQuestConclusionContents.Count != 3))
            {
                throw new ArgumentException("Battlepack Section 38: 'Pre Quest Conclusion Contents' must have exactly 3 entries.");
            }

            if (entries.Values.Any(i => i.PostQuestConclusionContents.Count != 3))
            {
                throw new ArgumentException("Battlepack Section 38: 'Post Quest Conclusion Contents' must have exactly 3 entries.");
            }

            Entries = entries;
            SetupHeader((uint)entries.Count, 0x0C);
        }

        public Rewards(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < EntryCount; i++)
            {
                var entry = new Entry();
                for (var j = 0; j < 3; j++)
                {
                    var content = br.ReadUInt16();
                    entry.PreQuestConclusionContents.Add($"Content {j}", content);
                }

                for (var j = 0; j < 3; j++)
                {
                    var content = br.ReadUInt16();
                    entry.PostQuestConclusionContents.Add($"Content {j}", content);
                }
                Entries.Add($"Reward {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var entry in Entries.Values)
            {
                foreach (var content in entry.PreQuestConclusionContents.Values)
                {
                    bw.Write(content);
                }

                foreach (var content in entry.PostQuestConclusionContents.Values)
                {
                    bw.Write(content);
                }
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Entry
        {
            [JsonPropertyName("Pre Quest Conclusion Contents")]
            public Dictionary<string, ushort> PreQuestConclusionContents { get; set; }

            [JsonPropertyName("Post Quest Conclusion Contents")]
            public Dictionary<string, ushort> PostQuestConclusionContents { get; set; }

            public Entry()
            {
                PreQuestConclusionContents = new Dictionary<string, ushort>();
                PostQuestConclusionContents = new Dictionary<string, ushort>();
            }
        }
    }
}