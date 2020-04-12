using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainJar
{
    /// <summary>
    /// Underlying data Sections are a nightmare
    /// to work with, this abstracts them into
    /// something sane.
    /// </summary>
    public class SaneSection
    {
        private readonly IDictionary<int, Block> Palette;

        private readonly int IndexBits;

        public ICollection<PlacedBlock> PlacedBlocks { get; set; } = new List<PlacedBlock>();

        /// <summary>
        /// True offset, not index
        /// </summary>
        public readonly int YOffset;
        /// <summary>
        /// Y Index (YOffset / 16)
        /// </summary>
        public readonly int YIndex;

        public SaneSection(Chunk.ChunkLevel.LevelSection section)
        {
            var dumping = false;
            if (section.PaletteRaw != null && section.PaletteRaw.ToString().Contains("diamond_block"))
            {
                Console.WriteLine("Found a diamond section");
                Console.WriteLine(section.PaletteRaw);
                Console.WriteLine();
                dumping = true;
            }

            YIndex = section.Y;
            YOffset = section.Y * 16;

            if (section.Palette == null)
            {
                return;
            }

            Palette = section
                .Palette
                .Select(a => new Block
                {
                    Name = a.Name,
                    Properties = a.Properties
                })
                .Select((a, i) => KeyValuePair.Create(i, a))
                .ToDictionary(a => a.Key, a => a.Value);

            IndexBits = Math.Max(
                4, // Minimum index size
                (int)Math.Ceiling(Math.Log2(Palette.Count)) // bits required to index palette
            );

            // Convert the long array to a bit array
            var stateIndices = section
                .BlockStates
                .ToBits()
                .Batch(IndexBits);

            foreach (var (entry, i) in stateIndices.WithIndex())
            {
                var index = 0;
                foreach (var bit in entry)
                {
                    // idempotent on first run
                    index <<= 1;
                    if (bit)
                    {
                        index |= 1;
                    }
                }

                var scalarCoordinates = i;
                var x = scalarCoordinates % 16;
                scalarCoordinates /= 16;
                var z = scalarCoordinates % 16;
                scalarCoordinates /= 16;
                var y = scalarCoordinates % 16;

                // According to this, an index without a matching
                // value is considered air. Not sure if that holds
                // but we'll run with it
                // https://wiki.vg/Chunk_Format
                if (!Palette.TryGetValue(index, out var block))
                {
                    if (dumping)
                    {
                        Console.Write("miss!  ");
                    }

                    block = new Block
                    {
                        Name = BlockTypes.Air.ToMinecraftNamespace()
                    };
                }

                if (dumping)
                {
                    Console.WriteLine($"{x},{y},{z} : {block.Name}");
                }

                PlacedBlocks.Add(new PlacedBlock
                {
                    XOffset = x,
                    YOffset = y,
                    ZOffset = z,
                    Block = block
                });
            }
        }
    }

    // I think this is superfluous and we can use the same definition from the DTO
    public class Block
    {
        public string Name { get; set; }
        public IDictionary<string, string> Properties { get; set; }
    }

    public class PlacedBlock
    {
        public int XOffset { get; set; }
        public int YOffset { get; set; }
        public int ZOffset { get; set; }

        public Block Block { get; set; }
    }
}
