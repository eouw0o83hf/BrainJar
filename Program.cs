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
        public static async Task Main(string[] _)
        {
            var manager = new RegionManager();

            /*
                long line (~20ish) of nether brick. one is at -253, 64, -233 to x-value -220
                cement and glass structure surrounded by four nether bricks
                nether blocks at
                    -253, 64, -200
                    -257, 64, -200
                    -257, 64, -206
                    -253, 64, -206
            */

            var diamondBlocks = manager.Search(BlockTypes.BlockOfDiamond, -253, 64, -200);

            await foreach (var block in diamondBlocks)
            {
                Console.WriteLine($"({block.X},{block.Y},{block.Z}) {block.Block.Name}");
            }
        }
    }
}
