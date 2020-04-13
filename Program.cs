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

            await manager.LoadMegacube(-253, 64, -200);

            Console.WriteLine("Ready for input");

            while (true)
            {
                var coords = Console
                    .ReadLine()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();
                var block = manager.GetBlock2(coords[0], coords[1], coords[2]);

                if (block == null)
                {
                    Console.WriteLine("No block found");
                }
                else
                {
                    Console.WriteLine(block.Block.Name);
                    if (block.Block.Properties != null)
                    {
                        foreach (var x in block.Block.Properties)
                        {
                            Console.WriteLine($"  {x.Key}: {x.Value}");
                        }
                    }
                }
            }
        }
    }
}
