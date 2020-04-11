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
        /// <summary>
        /// Version of the Chunk NBT structure
        /// </summary>
        public readonly int DataVersion;

        /// <summary>
        /// Core Chunk data
        /// </summary>
        public readonly ChunkLevel Level;

        public Chunk(NbtFile data)
        {
            DataVersion = data.RootTag.Get<NbtInt>("DataVersion").Value;
            Level = new ChunkLevel(data.RootTag.Get<NbtCompound>("Level"));
        }

        public class ChunkLevel
        {
            /// <summary>
            /// X position of this Chunk
            /// </summary>
            public readonly int XPos;

            /// <summary>
            /// Z position of this Chunk
            /// </summary>
            public readonly int ZPos;

            public readonly long LastUpdate;
            public readonly long InhabitedTime;

            public readonly int[] Biomes;

            /// <summary>
            /// Several different heightmaps corresponding to 256 values compacted at 9 bits per value (lowest being 0, highest being 256, both values inclusive)
            /// </summary>
            /// <remarks>
            /// This feels like caching/optimization.
            /// I suspect we may be able to ignore this and
            /// let the game recalculate but that may be
            /// overly optimistic. Many chunks have null
            /// values inside, so that backs up my guess.
            /// </remarks>
            public readonly LevelHeightmap Heightmaps;
            public readonly LevelCarvingMasks CarvingMasks;

            /// <summary>
            /// A sub-cube of the Chunk, covering 16
            /// blocks in the y direction
            /// </summary>
            public readonly IReadOnlyCollection<LevelSection> Sections;

            public readonly NbtList Entities;
            public readonly NbtList TileEntities;
            public readonly NbtList TileTicks;
            public readonly NbtList LiquidTicks;
            public readonly NbtList Lights;
            public readonly NbtList LiquidsToBeTicked;
            public readonly NbtList ToBeTicked;
            public readonly NbtList PostProcessing;

            public readonly string Status;

            public readonly NbtCompound Structures;

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

                Entities = data.Get<NbtList>("Entities");
                TileEntities = data.Get<NbtList>("TileEntities");
                TileTicks = data.Get<NbtList>("TileTicks");
                LiquidTicks = data.Get<NbtList>("LiquidTicks");
                Lights = data.Get<NbtList>("Lights");
                LiquidsToBeTicked = data.Get<NbtList>("LiquidsToBeTicked");
                ToBeTicked = data.Get<NbtList>("ToBeTicked");
                PostProcessing = data.Get<NbtList>("PostProcessing");

                Status = data.Get<NbtString>("Status")?.Value;

                Structures = data.Get<NbtCompound>("Structures");
            }

            public class LevelHeightmap
            {
                /// <summary>
                /// The highest block that blocks motion or contains a fluid.
                /// </summary>
                public readonly long[] MotionBlocking;
                /// <summary>
                /// The highest block that blocks motion or contains a fluid or is in the minecraft:leaves tag.
                /// </summary>
                public readonly long[] MotionBlockingNoLeaves;
                /// <summary>
                /// The highest non-air block, solid block.
                /// </summary>
                public readonly long[] OceanFloor;
                /// <summary>
                /// The highest block that is neither air nor contains a fluid, for worldgen.
                /// </summary>
                public readonly long[] OceanFloorWg;
                /// <summary>
                /// The highest non-air block.
                /// </summary>
                public readonly long[] WorldSurface;
                /// <summary>
                /// The highest non-air block, for worldgen.
                /// </summary>
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
                /// <summary>
                /// Y index of this section
                /// </summary>
                /// <remarks>
                /// Pretty sure this needs to be multiplied by 16
                /// since it's the index and not the coordinate.
                /// It seems like there's always an entry for 255
                /// to cap off the top of the world(?) maybe
                /// </remarks>
                public readonly byte Y;

                /// <summary>
                /// Set of different block states used in the chunk
                /// </summary>
                /// <remarks>
                /// I have a conjecture that Palette is a misnomer
                /// and that it's actually the core data for the
                /// blocks in this section
                /// </remarks>
                public readonly IReadOnlyCollection<SectionBlock> Palette;
                public readonly NbtList PaletteRaw;

                public readonly byte[] BlockLight;

                /// <raw>
                /// A variable amount of 64-bit longs, enough to fit 4096 indices. The indices correspond to the ordering of elements in the section's Palette. All indices are the same size in a section, which is the size required to represent the largest index (minimum of 4 bits). If the size of each index is not a factor of 64, the bits continue on the next long.
                /// </raw>
                public readonly long[] BlockStates;
                public readonly byte[] SkyLight;

                public LevelSection(NbtCompound data)
                {
                    Y = data.Get<NbtByte>("Y").Value;

                    PaletteRaw = data.Get<NbtList>("Palette");
                    Palette = PaletteRaw
                        ?.Cast<NbtCompound>()
                        .Select(a => new SectionBlock(a))
                        .ToList()
                        .AsReadOnly();

                    BlockLight = data.Get<NbtByteArray>("BlockLight")?.Value;
                    BlockStates = data.Get<NbtLongArray>("BlockStates")?.Value;
                    SkyLight = data.Get<NbtByteArray>("SkyLight")?.Value;
                }

                public class SectionBlock
                {
                    /// <summary>
                    /// Namespaced block ID
                    /// </summary>
                    public readonly string Name;
                    /// <summary>
                    /// List of block state properties
                    /// </summary>
                    /// <remarks>
                    /// To reserialize, just jame each kvp into the
                    /// Properties compound as an NbtString with
                    /// Name = Key and Value = Value
                    /// </remarks>
                    public readonly IDictionary<string, string> Properties;

                    public SectionBlock(NbtCompound data)
                    {
                        Name = data.Get<NbtString>("Name").Value;

                        Properties = data
                            .Get<NbtCompound>("Properties")
                            ?.OfType<NbtString>()
                            .ToDictionary(a => a.Name, a => a.Value);
                    }
                }
            }
        }
    }
}
