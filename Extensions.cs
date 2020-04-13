using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainJar
{
    public static class Extensions
    {
        /// <summary>
        /// Decompresses the bits contained
        /// in the source longs with sub-group
        /// lengths groupSize. Big-endian
        /// </summary>
        public static IEnumerable<int> DecompressInts(this long[] source, int groupSize)
            => source
                .ToBitGroups(groupSize)
                .ToInts();

        /// <summary>
        /// Converts the given long array into
        /// a series of bit groupings. Big-endian
        /// </summary>
        public static IEnumerable<IEnumerable<bool>> ToBitGroups(this long[] source, int groupSize)
            => source
                .ToBits()
                .Batch(groupSize);

        public static IEnumerable<int> ToInts(this IEnumerable<IEnumerable<bool>> source)
        {
            foreach (var group in source)
            {
                var acc = 0;
                foreach (var bit in group)
                {
                    // idempotent on first run
                    acc <<= 1;
                    if (bit)
                    {
                        acc |= 1;
                    }
                }
                yield return acc;
            }
        }

        /// <summary>
        /// Splits the input IEnumerable into sections which are all of size 'size'
        /// (except for maybe the last section depending on how the division works)
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            // Stole code from here because, while it seemed fun to write,
            // I didn't want to spend the time.
            // https://stackoverflow.com/a/13710023/570190
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return YieldBatchElements(enumerator, batchSize - 1);
                }
            }
        }

        private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 0; i < batchSize && source.MoveNext(); i++)
            {
                yield return source.Current;
            }
        }

        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> source)
            => source.Select((a, i) => (a, i));

        public static IEnumerable<bool> ToBits(this IEnumerable<long> source)
            => source
                .SelectMany(ToBytes)
                .SelectMany(ToEndianBits);

        public static IEnumerable<byte> ToBytes(this long source)
        {
            // I think we need to go from large to small
            // but I'm not confident. Just make sure it's
            // consistent with splitting into bits.
            var bytes = BitConverter.GetBytes(source);
            if (BitConverter.IsLittleEndian)
            {
                return bytes.Reverse();
            }
            return bytes;
        }

        public static IEnumerable<bool> ToBits(this byte source)
        {
            var accumulator = source;
            for (var i = 0; i < 8; ++i)
            {
                yield return Convert.ToBoolean(accumulator & 1);
                accumulator >>= 1;
            }
        }

        public static IEnumerable<bool> ToEndianBits(this byte source)
        {
            var bits = source.ToBits();
            if (BitConverter.IsLittleEndian)
            {
                return bits.Reverse();
            }
            return bits;
        }

        public static string ToMinecraftNamespace(this string source)
            => $"minecraft:{source}";
    }
}
