using System;

namespace BrainJar
{
    public class ChunkLocation
    {
        public readonly int XOffset;
        public readonly int ZOffset;

        // Offset in 4k increments from the start
        // of the file. Left-shift 12 to convert to
        // actual bit-count offset from file start
        public readonly int Offset;
        // Length of the chunk, in 4k increments
        public readonly byte Size;

        public ChunkLocation(int index, byte[] buffer)
        {
            // TODO this might need to be swapped
            // since anvil format may have switched
            // ordering of indices
            XOffset = index % 32;
            ZOffset = index / 32;

            var offsetBytes = new byte[4];
            Array.Copy(buffer, 0, offsetBytes, 1, 3);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(offsetBytes);
            }
            Offset = BitConverter.ToInt32(offsetBytes);

            Size = buffer[3];
        }
    }

    public class ChunkTimestamp
    {
        public readonly int XOffset;
        public readonly int ZOffset;

        // In epoch seconds
        public readonly uint TimestampRaw;

        public ChunkTimestamp(int index, byte[] buffer)
        {
            XOffset = index % 32;
            ZOffset = index / 32;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            TimestampRaw = BitConverter.ToUInt32(buffer);
        }

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime Timestamp => _unixEpoch.AddSeconds(TimestampRaw);
    }
}
