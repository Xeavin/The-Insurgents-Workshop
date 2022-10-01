using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Ard
{
    public class SpecialActionAnimations
    {
        [JsonPropertyName("Special Action Animations")]
        public Dictionary<string, Entry> Entries { get; set; }

        [JsonConstructor]
        public SpecialActionAnimations(Dictionary<string, Entry> entries)
        {
            Entries = entries;
        }

        public SpecialActionAnimations(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            var entryCount = br.ReadUInt32();
            br.BaseStream.Seek(0x10, SeekOrigin.Begin);
            Entries = new Dictionary<string, Entry>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = new Entry
                {
                    Model = br.ReadInt32(),
                    WeaponStance = br.ReadUInt16(),
                    ActionCharacterAnimationLink = br.ReadUInt16(),
                    AnimationFileLink = br.ReadUInt16()
                };
                br.BaseStream.Seek(0x06, SeekOrigin.Current);
                Entries.Add($"Special Action Animation {i}", entry);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            if (Entries.Count == 0)
            {
                bw.Write(new byte[0x20]);
                return;
            }

            bw.Write(Entries.Count);
            bw.BaseStream.Seek(0x10, SeekOrigin.Begin);
            foreach (var entry in Entries.Values)
            {
                bw.Write(entry.Model);
                bw.Write(entry.WeaponStance);
                bw.Write(entry.ActionCharacterAnimationLink);
                bw.Write(entry.AnimationFileLink);
                bw.Write(new byte[0x06]);
            }
        }

        public class Entry
        {
            [JsonPropertyName("Model")]
            public int Model { get; set; }

            [JsonPropertyName("Weapon Stance")]
            public ushort WeaponStance { get; set; }

            [JsonPropertyName("Action Character Animation Link")]
            public ushort ActionCharacterAnimationLink { get; set; }

            [JsonPropertyName("Animation File Link")]
            public ushort AnimationFileLink { get; set; }
        }
    }
}
