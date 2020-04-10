using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            var individualRegion = "r.0.0.mca";
            var regionPath = Path.Combine(
                    DropboxPath,
                    "region",
                    individualRegion);

            var region = await RegionFile.Load(regionPath);

            // var origin = region.Chunks.Single(a => a.XOffset == 0 && a.ZOffset == 0);
            var nethers = region.Chunks.Where(a => a.ToString().Contains("glow", StringComparison.InvariantCultureIgnoreCase));
            foreach (var nnnn in nethers)
            {
                Console.WriteLine("Glowstone at " + nnnn.XOffset + ", " + nnnn.ZOffset);
            }
        }
    }
}
