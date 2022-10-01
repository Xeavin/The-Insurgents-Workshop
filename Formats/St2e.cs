using System;
using System.IO;
using System.Linq;

namespace Formats
{
    public class St2e
    {
        protected readonly byte[] Magic = { 0x73, 0x74, 0x32, 0x65 }; //st2e
        protected uint EntryCount { get; set; }
        protected ushort EntrySize { get; set; }
        protected uint EntrySectionOffset { get; set; } //always 0x20, unless entry count is 0.
        protected uint UnknownOffset0 { get; set; }
        protected uint TextSectionOffset { get; set; }
        protected uint UnknownOffset1 { get; set; }
        protected uint UnknownOffset2 { get; set; }

        public void ReadHeader(BinaryReader br)
        {
            if (!br.ReadBytes(4).SequenceEqual(Magic))
            {
                throw new ArgumentException("St2e: Unexpected magic.");
            }

            EntryCount = br.ReadUInt32();
            EntrySize = br.ReadUInt16();
            br.BaseStream.Seek(0x02, SeekOrigin.Current);
            EntrySectionOffset = br.ReadUInt32();
            UnknownOffset0 = br.ReadUInt32();
            TextSectionOffset = br.ReadUInt32();
            UnknownOffset1 = br.ReadUInt32();
            UnknownOffset2 = br.ReadUInt32();
        }

        public void SetupHeader(uint entryCount, ushort entrySize, uint unknownOffset0 = 0, uint textSectionOffset = 0, uint unknownOffset1 = 0, uint unknownOffset2 = 0)
        {
            EntryCount = entryCount;
            EntrySize = entrySize;
            EntrySectionOffset = entryCount == 0 ? 0 : (uint)0x20;
            UnknownOffset0 = unknownOffset0;
            TextSectionOffset = textSectionOffset;
            UnknownOffset1 = unknownOffset1;
            UnknownOffset2 = unknownOffset2;
        }

        public void WriteHeader(BinaryWriter bw)
        {
            bw.Write(Magic);
            bw.Write(EntryCount);
            bw.Write(EntrySize);
            bw.BaseStream.Seek(0x02, SeekOrigin.Current);
            bw.Write(EntrySectionOffset);
            bw.Write(UnknownOffset0);
            bw.Write(TextSectionOffset);
            bw.Write(UnknownOffset1);
            bw.Write(UnknownOffset2);
        }
    }
}
