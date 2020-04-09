using System;
using System.Collections.Generic;
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

        public static async Task Main(string[] args)
        {
            var individualRegion = "r.0.0.mca";
            var regionPath = Path.Combine(
                    DropboxPath,
                    "region",
                    individualRegion);

            var region = await RegionFile.Load(regionPath);

            var locations = region.ChunkLocations.Take(10).ToList();
            var timestamps = region.ChunkTimestamps.Take(10).ToList();
            for (var i = 0; i < 10; ++i)
            {
                Console.WriteLine(i);
                Console.WriteLine($"   x, z        | {locations[i].XOffset}, {locations[i].ZOffset}");
                Console.WriteLine($"   Offset Size | {locations[i].Offset}  {locations[i].Size}");
                Console.WriteLine($"   Timestamp   | {timestamps[i].Timestamp}");
            }
        }
    }

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

        public readonly IReadOnlyCollection<Chunk> Chunks;

        public RegionFile(int xAnchor, int zAnchor,
                          IReadOnlyCollection<ChunkLocation> chunkLocations,
                          IReadOnlyCollection<ChunkTimestamp> chunkTimestamps,
                          IReadOnlyCollection<Chunk> chunks)
        {
            XAnchor = xAnchor;
            ZAnchor = zAnchor;

            ChunkLocations = chunkLocations;
            ChunkTimestamps = chunkTimestamps;

            Chunks = chunks;
        }

        public static async Task<RegionFile> Load(string path)
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
            var chunks = new List<Chunk>();
            for (var i = 0; i < 1024; ++i)
            {
                var location = locations[i];
                var length = location.Size << 12;
                var buffer = new byte[length];

                stream.Seek(location.Offset << 12, SeekOrigin.Begin);
                await stream.ReadAsync(buffer, 0, length);

                chunks.Add(new Chunk(location.XOffset, location.ZOffset, buffer));
            }

            return new RegionFile(xAnchor, zAnchor, locations, timestamps, chunks);
        }
    }

    public class ChunkLocation
    {
        public readonly int XOffset;
        public readonly int ZOffset;

        // Offset in 4k increments from the start
        // of the file. Left-shift 12 to convert to
        // actual bit-count offset from file start
        public readonly int Offset;
        // Length of the chunk, in 4k increments
        public readonly byte Size;

        public ChunkLocation(int index, byte[] buffer)
        {
            // TODO this might need to be swapped
            // since anvil format may have switched
            // ordering of indeces
            XOffset = index % 32;
            ZOffset = index / 32;

            var offsetBytes = new byte[4];
            Array.Copy(buffer, 0, offsetBytes, 1, 3);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(offsetBytes);
            }
            Offset = BitConverter.ToInt32(offsetBytes);

            Size = buffer[3];
        }
    }

    public class ChunkTimestamp
    {
        public readonly int XOffset;
        public readonly int ZOffset;

        // In epoch seconds
        public readonly uint TimestampRaw;

        public ChunkTimestamp(int index, byte[] buffer)
        {
            XOffset = index % 32;
            ZOffset = index / 32;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            TimestampRaw = BitConverter.ToUInt32(buffer);
        }

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime Timestamp => _unixEpoch.AddSeconds(TimestampRaw);
    }

    /// <summary>
    /// A Chunk is a 16x16 X/Z segment with up
    /// to 256 in height on the Y axis
    /// </summary>
    public class Chunk
    {
        public readonly int XOffset;
        public readonly int ZOffset;

        public Chunk(int xOffset, int zOffset, byte[] buffer)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
        }
    }
}
