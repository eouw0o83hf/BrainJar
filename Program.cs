using System;
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
            var individualRegion = "r.0.0.mca";
            var regionPath = Path.Combine(
                    DropboxPath,
                    "region",
                    individualRegion);

            var region = await RegionFile.Load(regionPath);

            // var locations = region.ChunkLocations.Take(10).ToList();
            // var timestamps = region.ChunkTimestamps.Take(10).ToList();
            // for (var i = 0; i < 10; ++i)
            // {
            //     Console.WriteLine(i);
            //     Console.WriteLine("Offset " + (locations[i].Offset << 12));
            //     Console.WriteLine($"   x, z        | {locations[i].XOffset}, {locations[i].ZOffset}");
            //     Console.WriteLine($"   Offset Size | {locations[i].Offset}  {locations[i].Size}");
            //     Console.WriteLine($"   Timestamp   | {timestamps[i].Timestamp}");
            // }
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

            // var lastLocation = locations
            //     .Single(a => a.XOffset == 2
            //                 && a.ZOffset == 9);
            // {
            //     var length = lastLocation.Size << 12;
            //     var buffer = new byte[length];

            //     stream.Seek(lastLocation.Offset << 12, SeekOrigin.Begin);
            //     await stream.ReadAsync(buffer, 0, length);

            //     Console.WriteLine($"Reading from {lastLocation.Offset << 12} for {length}");

            //     new Chunk(lastLocation.XOffset, lastLocation.ZOffset, buffer);
            // }

            // Section II: Chunks
            //  Location and size defined by location
            //  found in header
            var successCount = 0;
            var failureCount = 0;

            var chunks = new List<Chunk>();
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

                try
                {
                    chunks.Add(new Chunk(location.XOffset, location.ZOffset, buffer));
                }
                catch
                {
                    ++failureCount;
                    // Console.WriteLine("Failure for (" + location.XOffset + ", " + location.ZOffset + ")");
                    continue;
                }

                ++successCount;

                // Console.WriteLine("It worked for (" + location.XOffset + ", " + location.ZOffset + ")");
            }

            Console.WriteLine(successCount + " succeeded");
            Console.WriteLine(failureCount + " failed");

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
            // ordering of indices
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

        public readonly int DataLength;

        public Chunk(int xOffset, int zOffset, byte[] buffer)
        {
            XOffset = xOffset;
            ZOffset = zOffset;

            /*
            Chunk data begins with a (big-endian) four-byte length field that indicates the exact length of the remaining chunk data in bytes. The following byte indicates the compression scheme used for chunk data, and the remaining (length-1) bytes are the compressed chunk data.

            Minecraft always pads the last chunk's data to be a multiple-of-4096B in length (so that the entire file has a size that is a multiple of 4KiB). Minecraft does not accept files in which the last chunk is not padded. Note that this padding is not included in the length field.            
            */

            var lengthArray = buffer[0..4];
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthArray);
            }
            DataLength = BitConverter.ToInt32(lengthArray);

            var nbt = new NbtFile();
            nbt.LoadFromBuffer(buffer, 5, DataLength - 5, NbtCompression.AutoDetect);
        }
    }

    public static class Extensions
    {
        public static void DumpArray(this byte[] array, int rows = 16, int startIndex = 0)
        {
            for (var i = 0; i < rows; ++i)
            {
                Console.WriteLine(
                    string.Join(' ',
                        array.Skip(startIndex + i * 16).Take(16)
                    )
                );
            }
        }
    }
}
