using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Environment;

namespace BrainJar
{
    class Program
    {
        private const string TargetSave = "CG Base";

        private static readonly string SavesPath = Path.Combine(
                Environment.GetFolderPath(SpecialFolder.ApplicationData),
                ".minecraft",
                "saves",
                TargetSave
            );

        private static readonly string DropboxPath = Path.Combine(
            Environment.GetFolderPath(SpecialFolder.UserProfile),
            "Dropbox",
            "Minecraft"
        );

        public static async Task Main(string[] args)
        {
            var individualRegion = "r.0.0.mca";
            var regionPath = Path.Combine(
                    DropboxPath,
                    "region",
                    individualRegion);

            using var stream = File.OpenRead(regionPath);
            var region = new RegionFile(stream);
        }
    }

    public class RegionFile
    {
        public readonly IReadOnlyCollection<RegionChunkLocation> ChunkLocations;
        public readonly IReadOnlyCollection<RegionChunkTimestamp> ChunkTimestamps;

        public RegionFile(IReadOnlyCollection<RegionChunkLocation> chunkLocations,
                          IReadOnlyCollection<RegionChunkTimestamp> chunkTimestamps)
        {
            ChunkLocations = chunkLocations;
            ChunkTimestamps = chunkTimestamps;
        }

        public static async Task<RegionFile> Load(Stream filestream)
        {
            // Section I:   Header
            //              8kb
            //  Part A:     Locations (1024 entries, 4 bytes apiece)
            //  Part B:     Timestamps (1024 entries, 4 bytes apiece)

            // IA. Load Locations
            var locations = new List<RegionChunkLocation>();
            for (var i = 0; i < 1024; ++i)
            {
                var buffer = new byte[4];
                await filestream.ReadAsync(buffer, 0, 4);

                locations.Add(
                    new RegionChunkLocation(buffer)
                );
            }

            // IB. Load Timestamps
            var timestamps = new List<RegionChunkTimestamp>();
            for (var i = 0; i < 1024; ++i)
            {
                var buffer = new byte[4];
                await filestream.ReadAsync(buffer, 0, 4);

                timestamps.Add(
                    new RegionChunkTimestamp(buffer)
                );
            }
        }
    }

    public class RegionChunkLocation
    {
        // Offset in 4k increments from the start
        // of the file
        public readonly int Offset;
        // Length of the chunk, in 4k increments
        public readonly byte Size;

        public RegionChunkLocation(byte[] buffer)
        {
            var offsetBytes = new byte[3];
            Array.Copy(buffer, offsetBytes, 3);
            Offset = BitConverter.ToInt32(offsetBytes);

            Size = buffer[3];
        }
    }

    public class RegionChunkTimestamp
    {
        // In epoch seconds
        public readonly uint TimestampRaw;

        public RegionChunkTimestamp(byte[] buffer)
            => TimestampRaw = BitConverter.ToUInt32(buffer);

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime Timestamp => _unixEpoch.AddSeconds(TimestampRaw);
    }
}
