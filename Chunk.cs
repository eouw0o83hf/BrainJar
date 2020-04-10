using System;
using System.Collections.Generic;
using System.Linq;
using fNbt;

namespace BrainJar
{
    // Pulling from here
    //  https://minecraft.gamepedia.com/Chunk_format#Block_format
    // Just nesting everything and overlapping
    // names because this is horrible
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
            public readonly int XPos;
            public readonly int ZPos;

            public readonly long LastUpdate;
            public readonly long InhabitedTime;

            public readonly int[] Biomes;

            public readonly LevelHeightmap Heightmaps;
            public readonly LevelCarvingMasks CarvingMasks;
            public readonly IReadOnlyCollection<LevelSection> Sections;

            public ChunkLevel(NbtCompound data)
            {
                XPos = data.Get<NbtInt>("xPos").Value;
                XPos = data.Get<NbtInt>("zPos").Value;

                LastUpdate = data.Get<NbtLong>("LastUpdate").Value;
                InhabitedTime = data.Get<NbtLong>("InhabitedTime").Value;

                Biomes = data.Get<NbtIntArray>("Biomes")?.Value;

                Heightmaps = new LevelHeightmap(data.Get<NbtCompound>("Heightmaps"));
                var carvingMasks = data.Get<NbtCompound>("CarvingMasks");
                CarvingMasks = carvingMasks == null ? null : new LevelCarvingMasks(carvingMasks);

                Sections = data
                    .Get<NbtList>("Sections")
                    .Cast<NbtCompound>()
                    .Select(a =>
                        new LevelSection(a)
                    )
                    .ToList()
                    .AsReadOnly();
            }

            public class LevelHeightmap
            {
                public readonly long[] MotionBlocking;
                public readonly long[] MotionBlockingNoLeaves;
                public readonly long[] OceanFloor;
                public readonly long[] OceanFloorWg;
                public readonly long[] WorldSurface;
                public readonly long[] WorldSurfaceWg;

                public LevelHeightmap(NbtCompound data)
                {
                    MotionBlocking = data.Get<NbtLongArray>("MOTION_BLOCKING")?.Value;
                    MotionBlockingNoLeaves = data.Get<NbtLongArray>("MOTION_BLOCKING_NO_LEAVES")?.Value;
                    OceanFloor = data.Get<NbtLongArray>("OCEAN_FLOOR")?.Value;
                    OceanFloorWg = data.Get<NbtLongArray>("OCEAN_FLOOR_WG")?.Value;
                    WorldSurface = data.Get<NbtLongArray>("WORLD_SURFACE")?.Value;
                    WorldSurfaceWg = data.Get<NbtLongArray>("WORLD_SURFACE_WG")?.Value;
                }
            }

            public class LevelCarvingMasks
            {
                public readonly byte[] Air;
                public readonly byte[] Liquid;

                public LevelCarvingMasks(NbtCompound data)
                {
                    Air = data.Get<NbtByteArray>("AIR").Value;
                    Liquid = data.Get<NbtByteArray>("LIQUID").Value;
                }
            }

            public class LevelSection
            {
                public readonly byte Y;

                public readonly IReadOnlyCollection<SectionBlock> Palette;

                public readonly byte[] BlockLight;
                public readonly long[] BlockStates;
                public readonly byte[] SkyLight;

                public LevelSection(NbtCompound data)
                {
                    Y = data.Get<NbtByte>("Y").Value;

                    Palette = data
                        .Get<NbtList>("Palette")
                        ?.Cast<NbtCompound>()
                        .Select(a => new SectionBlock(a))
                        .ToList()
                        .AsReadOnly();

                    BlockLight = data.Get<NbtByteArray>("BlockLight").Value;
                    BlockStates = data.Get<NbtLongArray>("BlockStates").Value;
                    SkyLight = data.Get<NbtByteArray>("SkyLight").Value;
                }

                public class SectionBlock
                {
                    public readonly string Name;
                    public readonly BlockProperties Properties;

                    public SectionBlock(NbtCompound data)
                    {
                        Name = data.Get<NbtString>("Name").Value;

                        var properties = data.Get<NbtCompound>("Properties");
                        Properties = new BlockProperties(properties);
                    }

                    public class BlockProperties
                    {
                        public readonly string Name;

                        public BlockProperties(NbtCompound data)
                        {
                            Name = data.Get<NbtString>("Name")?.Value;
                        }
                    }
                }
            }
        }
    }
}
