using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Collections.Generic
{
    internal static class IEnumerableExtensions
    {
        public static T[] AsArray<T>(this IEnumerable<T> values)
        {
            Contract.Assert(values != null);

            T[] array = values as T[];
            if (array == null)
            {
                array = values.ToArray();
            }
            return array;
        }
    }
}
