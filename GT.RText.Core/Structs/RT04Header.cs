﻿using System;
using GT.Shared;
using GT.Shared.Polyphony;

namespace GT.RText.Core.Structs
{
    public struct RT04Header
    {
        public uint Magic { get; set; }
        public ulong Padding { get; set; }
        public uint EntryCount { get; set; }

        public RT04Header(EndianBinReader reader)
        {
            reader.BaseStream.Position = 0;
            if ((Magic = reader.ReadUInt32(EndianType.BIG_ENDIAN)) != Constants.RT04_MAGIC)
                throw new Exception("Invalid magic, doesn't match RT04.");

            Padding = reader.ReadUInt64();
            EntryCount = reader.ReadUInt32();
        }

        public void Save(EndianBinWriter writer)
        {
            var magicBytes = BitConverter.GetBytes(Magic);
            Array.Reverse(magicBytes);
            writer.Write(magicBytes);
            writer.Write(Padding);
            writer.Write(EntryCount);
        }
    }
}
