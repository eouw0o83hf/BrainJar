using System;
using fNbt;

namespace BrainJar
{
    /// <summary>
    /// A Chunk is a 16x16 X/Z segment with up
    /// to 256 in height on the Y axis
    /// </summary> 
    public class ChunkWrapper
    {
        public readonly int XOffset;
        public readonly int ZOffset;

        public readonly int DataLength;
        public readonly NbtFile Data;

        public readonly Chunk Chunk;

        public ChunkWrapper(int xOffset, int zOffset, byte[] buffer)
        {
            XOffset = xOffset;
            ZOffset = zOffset;

            /*
            Chunk data begins with a (big-endian) four-byte length field that indicates the exact length of the remaining chunk data in bytes. The following byte indicates the compression scheme used for chunk data, and the remaining (length-1) bytes are the compressed chunk data.

            Minecraft always pads the last chunk's data to be a multiple-of-4096B in length (so that the entire file has a size that is a multiple of 4KiB). Minecraft does not accept files in which the last chunk is not padded. Note that this padding is not included in the length field.            
            */

            var lengthArray = buffer[0..4];
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthArray);
            }
            DataLength = BitConverter.ToInt32(lengthArray);

            Data = new NbtFile();
            Data.LoadFromBuffer(buffer, 5, DataLength - 5, NbtCompression.AutoDetect);

            Chunk = new Chunk(Data);
        }
    }
}
