using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BrainJar
{
    public class RegionManager
    {
        // Name of target world
        private const string TargetSave = "CG Base";

        // For actual saves
        private static readonly string SavesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft",
                "saves",
                TargetSave
            );

        // For experimental target
        private static readonly string DropboxPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Dropbox",
            "Minecraft",
            TargetSave
        );

        private readonly List<RegionFile> Regions = new List<RegionFile>();

        /// <summary>
        /// Searches for the given block type (un-namespaced)
        /// in the megacube of 9 regions centered on the
        /// given block
        /// </summary>
        /// <param name="blockType">Un-namespaced block type (from BlockTypes.cs)</param>
        /// <param name="x">Approximate x location</param>
        /// <param name="y">Approximate y location</param>
        /// <param name="z">Approximate z location</param>
        /// <returns></returns>
        public async IAsyncEnumerable<AbsolutelyPlacedBlock> Search(string blockType, int x, int y, int z)
        {
            Regions.Clear();
            await LoadMegacube(x, y, z);

            var searchTerm = $"minecraft:{blockType}";

            foreach (var region in Regions)
            {
                foreach (var chunk in region.Chunks.Select(a => a.Chunk.Level))
                {
                    foreach (var section in chunk.SaneSections)
                    {
                        var xOffset = chunk.XPos * 16;
                        var yOffset = section.YOffset;
                        var zOffset = chunk.ZPos * 16;

                        var blocks = section
                            .PlacedBlocks
                            .Where(a => a.Block?.Name == searchTerm);
                        foreach (var block in blocks)
                        {
                            yield return new AbsolutelyPlacedBlock(
                                xOffset + block.XOffset,
                                yOffset + block.YOffset,
                                zOffset + block.ZOffset,
                                block.Block
                            );
                        }
                    }
                }
            }
        }

        public PlacedBlock GetBlock2(int x, int y, int z)
        {
            // Really fucking lazy but I don't feel like doing the math right now
            foreach (var region in Regions)
            {
                foreach (var chunk in region.Chunks.Select(a => a.Chunk.Level))
                {
                    foreach (var section in chunk.SaneSections)
                    {
                        var xOffset = chunk.XPos * 16;
                        var yOffset = section.YOffset;
                        var zOffset = chunk.ZPos * 16;

                        var blockX = x - xOffset;
                        var blockY = y - yOffset;
                        var blockZ = z - zOffset;

                        var match = section.PlacedBlocks.SingleOrDefault(a => a.XOffset == blockX && a.YOffset == blockY && a.ZOffset == blockZ);
                        if (match != null)
                        {
                            return match;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Loads the region containing the specified coordinate
        /// and all contiguous regions which exist in the
        /// regions folder. Loads up to 9 total regions.
        /// </summary>
        public async Task LoadMegacube(int x, int y, int z)
        {
            await GetRegion(x, z);
            // for (var i = -1; i <= 1; ++i)
            // {
            //     for (var j = -1; j <= 1; ++j)
            //     {
            //         await GetRegion(x + (i * 512), z + (j * 512));
            //     }
            // }
        }

        public async Task<PlacedBlock> GetBlock(int x, int y, int z)
        {
            var region = await GetRegion(x, z);

            // Specifies the relative x/z position of the block
            // within the frame of reference of its containing Region
            // 32 chunks per region per lateral dimension
            // 16 blocks per chunk per dimension
            // 32 * 16 = 512
            var xRegionOffset = x % 512;
            var zRegionOffset = z % 512;

            // Regions comprise 16x16 Chunks; use these to
            // lookup the correct Chunk by index
            var xChunkIndex = xRegionOffset / 16;
            var zChunkIndex = zRegionOffset / 16;

            // Specifies the relative x/z position of the block
            // within the frame of reference of its containing Chunk
            var xChunkOffset = xRegionOffset % 16;
            var zChunkOffset = zRegionOffset % 16;

            // Chunks comprise 16 vertically stacked Sections;
            // use this to lookup the correct Section by index
            var ySectionIndex = y / 16;
            var ySectionOffset = y % 16;

            var chunk = region
                .Chunks
                .SingleOrDefault(a => a.XOffset == xChunkIndex
                                        && a.ZOffset == zChunkIndex);

            var section = chunk
                ?.Chunk
                .Level
                .SaneSections
                .SingleOrDefault(a => a.YIndex == ySectionIndex);
            if (section == null)
            {
                return null;
            }

            return section
                .PlacedBlocks
                .SingleOrDefault(a =>
                    a.XOffset == xChunkOffset
                    && a.YOffset == ySectionOffset
                    && a.ZOffset == zChunkOffset
                );
        }

        /// <summary>
        /// Loads the region containing the specified x/z coordinates
        /// </summary>
        /// <param name="x">x coordinate (absolute) of a block inside the region</param>
        /// <param name="z">z coordinate (absolute) of a block inside the region</param>
        public async Task<RegionFile> GetRegion(int x, int z)
        {
            var xAnchor = x >> 9;
            var zAnchor = z >> 9;

            var region = Regions.SingleOrDefault(a => a.XAnchor == xAnchor && a.ZAnchor == zAnchor);
            if (region != null)
            {
                return region;
            }

            var filename = $"r.{xAnchor}.{zAnchor}.mca";
            var regionPath = Path.Combine(DropboxPath, "region", filename);
            if (!File.Exists(regionPath))
            {
                return null;
            }

            region = await RegionFile.LoadAsync(regionPath);

            Regions.Add(region);
            return region;
        }
    }

    public class AbsolutelyPlacedBlock
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public readonly Block Block;

        public AbsolutelyPlacedBlock(int x, int y, int z, Block block)
        {
            X = x;
            Y = y;
            Z = z;
            Block = block;
        }
    }
}
