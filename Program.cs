using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fNbt;

namespace BrainJar
{
    class Program
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

        public static async Task Main(string[] _)
        {
            var levelRoot = "level.dat";
            var levelPath = Path.Combine(DropboxPath, levelRoot);
            var levelNbt = new NbtFile(levelPath);
            Console.WriteLine(levelNbt);
            Environment.Exit(0);

            var individualRegion = "r.0.0.mca";
            var regionPath = Path.Combine(
                    DropboxPath,
                    "region",
                    individualRegion);

            var region = await RegionFile.Load(regionPath);


            var inGameYValue = 69;

            var yIndex = inGameYValue / 16;

            var originChunk = region
                .Chunks
                .Single(a => a.XOffset == 0 && a.ZOffset == 0)
                .Chunk
                .Level;

            var sections = originChunk
                .Sections
                .Select(a => new SaneSection(a))
                .OrderBy(a => a.YOffset);

            var tenthLevel = sections
                .Single(a => a.YIndex == yIndex)
                .PlacedBlocks
                .Where(a => a.YOffset == inGameYValue % 16);
            foreach (var item in tenthLevel)
            {
                if (item.Block.Name.EndsWith(":air") || item.Block.Name.EndsWith("_air"))
                {
                    continue;
                }
                Console.WriteLine($"({item.XOffset}, {item.YOffset}, {item.ZOffset}): {item.Block.Name}");
            }

        }
    }
}
