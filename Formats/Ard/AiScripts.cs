using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Ard
{
    public class AiScripts
    {
        [JsonPropertyName("Scripts")]
        public Dictionary<string, Script> Scripts { get; set; }

        [JsonConstructor]
        public AiScripts(Dictionary<string, Script> scripts)
        {
            Scripts = scripts;
        }

        public AiScripts(string filename)
        {
            Scripts = new Dictionary<string, Script>();
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));

            var scriptCount = br.ReadUInt16();
            br.BaseStream.Seek(0x02, SeekOrigin.Current);

            var scriptOffsets = new List<uint>();
            for (var i = 0; i < scriptCount; i++)
            {
                scriptOffsets.Add(br.ReadUInt32());
            }

            for (var i = 0; i < scriptCount; i++)
            {
                br.BaseStream.Seek(scriptOffsets[i], SeekOrigin.Begin);
                var script = new Script();
                var groupEntryCounts = br.ReadBytes(32);

                for (var j = 0; j < groupEntryCounts.Length; j++)
                {
                    if (groupEntryCounts[j] == 0)
                    {
                        continue;
                    }

                    var group = new Group();
                    for (var k = 0; k < groupEntryCounts[j]; k++)
                    {
                        var entry = new Entry
                        {
                            Action = br.ReadUInt16(),
                            ActionParameter = br.ReadInt16(),
                            Flags = br.ReadUInt16(),
                            FirstCase = new Case
                            {
                                TargetType = br.ReadUInt16(),
                                TargetCondition = br.ReadUInt16(),
                                Parameter = br.ReadUInt16()
                            },
                            SecondCase = new Case
                            {
                                TargetType = br.ReadUInt16(),
                                TargetCondition = br.ReadUInt16(),
                                Parameter = br.ReadUInt16()
                            },
                            ThirdCase = new Case
                            {
                                TargetType = br.ReadUInt16(),
                                TargetCondition = br.ReadUInt16(),
                                Parameter = br.ReadUInt16()
                            }
                        };
                        group.Entries.Add($"Entry {k}", entry);
                    }
                    script.Groups.Add($"Group {j}", group);
                }
                Scripts.Add($"Script {i}", script);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));

            //write header
            bw.Write((ushort)Scripts.Count);
            bw.Write((ushort)0x64); //unknown/unused attribute
            bw.BaseStream.Seek(Scripts.Count * 0x04 + 0x04, SeekOrigin.Current); //preserve space for script offsets & end of section offset

            //write script data
            var scriptOffsets = new List<uint>();
            foreach (var script in Scripts.Values)
            {
                scriptOffsets.Add((uint)bw.BaseStream.Position);
                foreach (var group in script.Groups.Values)
                {
                    bw.Write((byte)group.Entries.Count);
                }
                bw.BaseStream.Seek(0x20 - script.Groups.Count, SeekOrigin.Current); //32 groups alignment

                foreach (var entry in script.Groups.Values.SelectMany(group => group.Entries.Values))
                {
                    bw.Write(entry.Action);
                    bw.Write(entry.ActionParameter);
                    bw.Write(entry.Flags);
                    bw.Write(entry.FirstCase.TargetType);
                    bw.Write(entry.FirstCase.TargetCondition);
                    bw.Write(entry.FirstCase.Parameter);
                    bw.Write(entry.SecondCase.TargetType);
                    bw.Write(entry.SecondCase.TargetCondition);
                    bw.Write(entry.SecondCase.Parameter);
                    bw.Write(entry.ThirdCase.TargetType);
                    bw.Write(entry.ThirdCase.TargetCondition);
                    bw.Write(entry.ThirdCase.Parameter);
                }
            }
            var endOfFileOffset = (uint)bw.BaseStream.Position; //without alignment
            BinaryHelper.Align(bw, 16);

            //write script offsets and end of file offset
            bw.BaseStream.Seek(0x04, SeekOrigin.Begin);
            foreach (var offset in scriptOffsets)
            {
                bw.Write(offset);
            }
            bw.Write(endOfFileOffset);
        }

        public class Script
        {
            [JsonPropertyName("Groups")]
            public Dictionary<string, Group> Groups { get; set; } //32 groups max

            public Script()
            {
                Groups = new Dictionary<string, Group>();
            }

            [JsonConstructor]
            public Script(Dictionary<string, Group> groups)
            {
                if (groups.Count > 32)
                {
                    throw new ArgumentException("Ard Section 3: 'Scripts -> Groups' cannot contain more than 32 entries.");
                }
                Groups = groups;
            }
        }

        public class Group
        {
            [JsonPropertyName("Entries")]
            public Dictionary<string, Entry> Entries { get; set; }

            public Group()
            {
                Entries = new Dictionary<string, Entry>();
            }

            [JsonConstructor]
            public Group(Dictionary<string, Entry> entries)
            {
                Entries = entries;
            }
        }

        public class Entry
        {
            [JsonPropertyName("Action")]
            public ushort Action { get; set; }

            [JsonPropertyName("Action Parameter")]
            public short ActionParameter { get; set; }

            [JsonPropertyName("Flags")]
            public ushort Flags { get; set; }

            [JsonPropertyName("1. Case")]
            public Case FirstCase { get; set; }

            [JsonPropertyName("2. Case")]
            public Case SecondCase { get; set; }

            [JsonPropertyName("3. Case")]
            public Case ThirdCase { get; set; }

            public Entry()
            {
                FirstCase = new Case();
                SecondCase = new Case();
                ThirdCase = new Case();
            }
        }

        public class Case
        {
            [JsonPropertyName("Target Type")]
            public ushort TargetType { get; set; }

            [JsonPropertyName("Target Condition")]
            public ushort TargetCondition { get; set; }

            [JsonPropertyName("Parameter")]
            public ushort Parameter { get; set; }
        }
    }
}
