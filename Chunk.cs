using System;
using fNbt;

namespace BrainJar
{
    public class Chunk
    {
        public readonly int DataVersion;
        public readonly ChunkLevel Level;

        public Chunk(NbtFile data)
        {
            DataVersion = data.RootTag.Get<NbtInt>("DataVersion").Value;
            Level = new ChunkLevel(data.RootTag.Get<NbtCompound>("Level"));
        }

        public class ChunkLevel
        {
            public ChunkLevel(NbtCompound data)
            {
            }
        }
    }
}
