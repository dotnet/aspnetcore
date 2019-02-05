using System.Collections.Generic;

namespace System.Linq
{
    internal static class LinqExtension
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] batch = null;
            var count = 0;

            foreach (var item in source)
            {
                if (batch == null)
                {
                    batch = new T[size];
                }

                batch[count++] = item;
                if (count != size)
                {
                    continue;
                }

                yield return batch;

                batch = null;
                count = 0;
            }

            if (batch != null && count > 0)
            {
                yield return batch.Take(count);
            }
        }
    }
}
