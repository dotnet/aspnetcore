using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Convert an ICollection to an array, removing null values. Fast path for case where there are no null values.
        /// </summary>
        public static T[] ToArrayWithoutNulls<T>(this ICollection<T> collection) where T : class
        {
            Contract.Assert(collection != null);

            T[] result = new T[collection.Count];
            int count = 0;
            foreach (T value in collection)
            {
                if (value != null)
                {
                    result[count] = value;
                    count++;
                }
            }
            if (count == collection.Count)
            {
                return result;
            }
            else
            {
                T[] trimmedResult = new T[count];
                Array.Copy(result, trimmedResult, count);
                return trimmedResult;
            }
        }
    }
}
