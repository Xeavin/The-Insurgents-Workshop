using Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class DefaultPartyMemberGambits : St2e
    {
        [JsonPropertyName("Default Party Member Gambits")]
        public Dictionary<string, Group> Groups { get; set; }

        [JsonConstructor]
        public DefaultPartyMemberGambits(Dictionary<string, Group> groups)
        {
            Groups = groups;
            SetupHeader((uint)groups.Count, 0x40);
        }

        public DefaultPartyMemberGambits(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            ReadHeader(br);

            br.BaseStream.Seek(EntrySectionOffset, SeekOrigin.Begin);
            Groups = new Dictionary<string, Group>();
            for (var i = 0; i < EntryCount; i++)
            {
                var partyMember = new Group();
                var targets = new ushort[12];
                for (var j = 0; j < targets.Length; j++)
                {
                    targets[j] = br.ReadUInt16();
                }

                br.BaseStream.Seek(0x08, SeekOrigin.Current);
                var actions = new ushort[12];
                for (var j = 0; j < actions.Length; j++)
                {
                    actions[j] = br.ReadUInt16();
                }

                br.BaseStream.Seek(0x08, SeekOrigin.Current);
                for (var j = 0; j < actions.Length; j++)
                {
                    var entry = new Entry
                    {
                        Target = targets[j],
                        Action = actions[j]
                    };
                    partyMember.Entries.Add($"Entry {j}", entry);
                }
                Groups.Add($"Group {i}", partyMember);
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteHeader(bw);

            foreach (var group in Groups.Values)
            {
                foreach (var entry in group.Entries.Values)
                {
                    bw.Write(entry.Target);
                }

                bw.BaseStream.Seek(0x08, SeekOrigin.Current);
                foreach (var entry in group.Entries.Values)
                {
                    bw.Write(entry.Action);
                }
                bw.Write(new byte[8]);
            }
            BinaryHelper.Align(bw, 16);
        }

        public class Group
        {
            [JsonPropertyName("Entries")]
            public Dictionary<string, Entry> Entries { get; set; }

            public Group()
            {
                Entries = new Dictionary<string, Entry>();
            }
        }

        public class Entry
        {
            [JsonPropertyName("Target")]
            public ushort Target { get; set; }

            [JsonPropertyName("Action")]
            public ushort Action { get; set; }
        }
    }
}