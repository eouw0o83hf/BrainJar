using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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

        private readonly HeightMap _heightMap;

        /// <summary>
        /// True offset, not index
        /// </summary>
        public readonly int YOffset;
        /// <summary>
        /// Y Index (YOffset / 16)
        /// </summary>
        public readonly int YIndex;

        public SaneSection(Chunk.ChunkLevel.LevelSection section, HeightMap heightMap)
        {
            _heightMap = heightMap;

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
            // var stateIndices = section
            //     .BlockStates
            //     .ToBitGroups(IndexBits);
            var stateIndices = section
                .BlockStates
                .DecompressInts(IndexBits)
                .WithIndex();

            foreach (var (index, i) in stateIndices)
            {
                var scalarCoordinates = i;
                var x = scalarCoordinates % 16;
                scalarCoordinates /= 16;
                var z = scalarCoordinates % 16;
                scalarCoordinates /= 16;
                var y = scalarCoordinates % 16;

                Block block = null;
                if (//_heightMap.HasBlock(x, y + YOffset, z) &&
                    Palette.ContainsKey(index))
                {
                    // if (y + YOffset == 70 && Palette[index].Name.Contains(BlockTypes.BlockOfDiamond))
                    // {
                    //     var entr2y = _heightMap.Entries.Single(a => a.XOffset == x && a.ZOffset == z);
                    //     Console.WriteLine("we got a rogue diamond here");
                    // }

                    block = Palette[index];
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
