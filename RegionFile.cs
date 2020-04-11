using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BrainJar
{
    /// <summary>
    /// A 32x32 grid of Chunks defined in
    /// an optimized storage format
    /// </summary>
    public class RegionFile
    {
        public readonly int XAnchor;
        public readonly int ZAnchor;

        public readonly IReadOnlyCollection<ChunkLocation> ChunkLocations;
        public readonly IReadOnlyCollection<ChunkTimestamp> ChunkTimestamps;

        public readonly IReadOnlyCollection<ChunkWrapper> Chunks;

        public RegionFile(int xAnchor, int zAnchor,
                          IReadOnlyCollection<ChunkLocation> chunkLocations,
                          IReadOnlyCollection<ChunkTimestamp> chunkTimestamps,
                          IReadOnlyCollection<ChunkWrapper> chunks)
        {
            XAnchor = xAnchor;
            ZAnchor = zAnchor;

            ChunkLocations = chunkLocations;
            ChunkTimestamps = chunkTimestamps;

            Chunks = chunks;
        }

        public static async Task<RegionFile> LoadAsync(string path)
        {
            // Filename is of form `r.#.#.mca`
            var filename = Path.GetFileName(path);
            var components = filename.Split('.');
            var xAnchor = int.Parse(components[1]) << 5;
            var zAnchor = int.Parse(components[2]) << 5;

            using var stream = File.OpenRead(path);

            // Section I:   Header
            //              8kb
            //  Part A:     Locations (1024 entries, 4 bytes apiece)
            //  Part B:     Timestamps (1024 entries, 4 bytes apiece)

            // IA. Load Locations
            var locations = new List<ChunkLocation>();
            for (var i = 0; i < 1024; ++i)
            {
                var buffer = new byte[4];
                await stream.ReadAsync(buffer, 0, 4);

                locations.Add(
                    new ChunkLocation(i, buffer)
                );
            }

            // IB. Load Timestamps
            var timestamps = new List<ChunkTimestamp>();
            for (var i = 0; i < 1024; ++i)
            {
                var buffer = new byte[4];
                await stream.ReadAsync(buffer, 0, 4);

                timestamps.Add(
                    new ChunkTimestamp(i, buffer)
                );
            }

            // Section II: Chunks
            //  Location and size defined by location
            //  found in header
            var chunks = new List<ChunkWrapper>();
            for (var i = 0; i < 1024; ++i)
            {
                var location = locations[i];
                var length = location.Size << 12;
                var buffer = new byte[length];

                if (location.Size == 0)
                {
                    continue;
                }

                stream.Seek(location.Offset << 12, SeekOrigin.Begin);
                await stream.ReadAsync(buffer, 0, length);

                chunks.Add(new ChunkWrapper(location.XOffset, location.ZOffset, buffer));
            }

            return new RegionFile(xAnchor, zAnchor, locations, timestamps, chunks);
        }
    }
}
