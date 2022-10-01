using Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Formats.Battlepack
{
    public class LicenseBoard
    {
        private readonly byte[] magic = { 0x6C, 0x69, 0x63, 0x64 }; //licd

        [JsonPropertyName("License Board Nodes")]
        public Dictionary<string, Dictionary<string, int>> Entries { get; set; }

        [JsonConstructor]
        public LicenseBoard(Dictionary<string, Dictionary<string, int>> entries)
        {
            var rowCount = entries.Values.Max(i => i.Count);
            if (entries.Values.Any(i => i.Count != rowCount))
            {
                throw new ArgumentException("Battlepack Section 70: All 'Column' entries must have the same 'Row' entry count.");
            }
            Entries = entries;
        }

        public LicenseBoard(string filename)
        {
            using var br = new BinaryReader(File.Open(filename, FileMode.Open));
            if (!br.ReadBytes(4).SequenceEqual(magic))
            {
                throw new ArgumentException("Battlepack Section 70: Unexpected magic.");
            }

            var columnCount = br.ReadUInt16();
            var rowCount = br.ReadUInt16();
            Entries = new Dictionary<string, Dictionary<string, int>>();
            for (var i = 0; i < columnCount; i++)
            {
                Entries.Add($"Column {i}", new Dictionary<string, int>());
            }

            for (var i = 0; i < rowCount; i++)
            {
                for (var j = 0; j < columnCount; j++)
                {
                    var value = br.ReadUInt16();
                    Entries[$"Column {j}"].Add($"Row {i}", value);
                }
            }
        }

        public void WriteToBinary(string filename)
        {
            using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            bw.Write(magic);
            var columnCount = (ushort)Entries.Count;
            var rowCount = (ushort)Entries.Values.Max(i => i.Count);
            bw.Write(columnCount);
            bw.Write(rowCount);

            for (var i = 0; i < rowCount; i++)
            {
                for (var j = 0; j < columnCount; j++)
                {
                    var value = (ushort)Entries[$"Column {j}"][$"Row {i}"];
                    bw.Write(value);
                }
            }
            BinaryHelper.Align(bw, 16);
        }
    }
}