using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainJar
{
    public class HeightMap
    {
        public readonly ICollection<HeightMapEntry> Entries
             = new List<HeightMapEntry>();

        public HeightMap(
            long[] motionBlocking,
            long[] motionBlockingNoLeaves,
            long[] oceanFloor,
            long[] oceanFloorWg,
            long[] worldSurface,
            long[] worldSurfaceWg
        )
        {
            return;

            var motionBlockingInts = motionBlocking?.DecompressInts(9).ToList();
            var motionBlockingNoLeavesInts = motionBlockingNoLeaves?.DecompressInts(9).ToList();
            var oceanFloorInts = oceanFloor?.DecompressInts(9).ToList();
            var oceanFloorWgInts = oceanFloorWg?.DecompressInts(9).ToList();
            var worldSurfaceInts = worldSurface?.DecompressInts(9).ToList();
            var worldSurfaceWgInts = worldSurfaceWg?.DecompressInts(9).ToList();

            // if (worldSurface != null)
            // {
            //     Console.WriteLine("World Surface");
            //     Console.WriteLine(
            //         string.Join(
            //             Environment.NewLine,
            //             worldSurface.Select(a => $"  {a}")
            //         )
            //     );

            //     var bits = worldSurface.ToBits().Batch(64);
            //     Console.WriteLine();
            //     Console.WriteLine("Bits");
            //     foreach (var f in bits)
            //     {
            //         Console.WriteLine(
            //             string.Join("", f.Select(a => a ? 1 : 0))
            //         );
            //     }

            //     Console.WriteLine();
            //     Console.WriteLine("Ints");
            //     foreach (var i in worldSurfaceInts)
            //     {
            //         Console.WriteLine(
            //             string.Join(
            //                 Environment.NewLine,
            //                 worldSurfaceInts.Select(a => $"  {a}")
            //             )
            //         );
            //     }

            //     Environment.Exit(0);
            // }

            for (int i = 0; i < 256; ++i)
            {
                var x = i % 16;
                var z = i / 16;

                Entries.Add(
                    new HeightMapEntry(x, z,
                        motionBlockingInts?[i],
                        motionBlockingNoLeavesInts?[i],
                        oceanFloorInts?[i],
                        oceanFloorWgInts?[i],
                        worldSurfaceInts?[i],
                        worldSurfaceWgInts?[i]
                    )
                );
            }
        }

        /// <summary>
        /// Given a coordinate triplet,
        /// determines based on the heightmap
        /// whether or not a solid block should
        /// exist in that cube of space
        /// </summary>
        public bool HasBlock(int x, int y, int z)
        {
            var entry = Entries.Single(a => a.XOffset == x && z == a.ZOffset);

            if (entry.MotionBlockingNoLeaves.HasValue)
            {
                return y < entry.MotionBlockingNoLeaves.Value;
            }
            if (entry.WorldSurface.HasValue)
            {
                return y < entry.WorldSurface.Value;
            }
            if (entry.OceanFloor.HasValue)
            {
                return y < entry.OceanFloor.Value;
            }

            // Um
            throw new Exception("no height map found");
        }
    }

    public class HeightMapEntry
    {
        /// <summary>
        /// X offset within the Chunk
        /// </summary>
        public readonly int XOffset;

        /// <summary>
        /// Z offset within the Chunk
        /// </summary>
        public readonly int ZOffset;

        /// <summary>
        /// The highest block that blocks motion or contains a fluid
        /// </summary>
        public readonly int? MotionBlocking;

        /// <summary>
        /// The highest block that blocks motion or contains a fluid or is in the minecraft:leaves tag
        /// </summary>
        public readonly int? MotionBlockingNoLeaves;

        /// <summary>
        /// The highest non-air block, solid block
        /// </summary>
        public readonly int? OceanFloor;

        /// <summary>
        /// The highest block that is neither air nor contains a fluid, for worldgen
        /// </summary>
        public readonly int? OceanFloorWg;

        /// <summary>
        /// The highest non-air block
        /// </summary>
        public readonly int? WorldSurface;

        /// <summary>
        /// The highest non-air block, for worldgen
        /// </summary>
        public readonly int? WorldSurfaceWg;

        public HeightMapEntry(int xOffset, int zOffset,
                         int? motionBlocking, int? motionBlockingNoLeaves,
                         int? oceanFloor, int? oceanFloorWg,
                         int? worldSurface, int? worldSurfaceWg)
        {
            XOffset = xOffset;
            ZOffset = zOffset;

            MotionBlocking = motionBlocking;
            MotionBlockingNoLeaves = motionBlockingNoLeaves;
            OceanFloor = oceanFloor;
            OceanFloorWg = oceanFloorWg;
            WorldSurface = worldSurface;
            WorldSurfaceWg = worldSurfaceWg;
        }
    }
}
