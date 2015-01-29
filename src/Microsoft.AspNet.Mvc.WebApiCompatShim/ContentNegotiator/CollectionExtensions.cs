// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Generic
{
    /// <summary>
    /// Helper extension methods for fast use of collections.
    /// </summary>
    internal static class CollectionExtensions
    {
        /// <summary>
        /// Return a new array with the value added to the end. Slow and best suited to long lived arrays with few
        /// writes relative to reads.
        /// </summary>
        public static T[] AppendAndReallocate<T>(this T[] array, T value)
        {
            Debug.Assert(array != null);

            var originalLength = array.Length;
            var newArray = new T[originalLength + 1];
            array.CopyTo(newArray, 0);
            newArray[originalLength] = value;
            return newArray;
        }

        /// <summary>
        /// Return the enumerable as an Array, copying if required. Optimized for common case where it is an Array.
        /// Avoid mutating the return value.
        /// </summary>
        public static T[] AsArray<T>(this IEnumerable<T> values)
        {
            Debug.Assert(values != null);

            var array = values as T[];
            if (array == null)
            {
                array = values.ToArray();
            }
            return array;
        }

        /// <summary>
        /// Return the enumerable as a Collection of T, copying if required. Optimized for the common case where it is
        /// a Collection of T and avoiding a copy if it implements IList of T. Avoid mutating the return value.
        /// </summary>
        public static Collection<T> AsCollection<T>(this IEnumerable<T> enumerable)
        {
            Debug.Assert(enumerable != null);

            var collection = enumerable as Collection<T>;
            if (collection != null)
            {
                return collection;
            }
            // Check for IList so that collection can wrap it instead of copying
            var list = enumerable as IList<T>;
            if (list == null)
            {
                list = new List<T>(enumerable);
            }
            return new Collection<T>(list);
        }

        /// <summary>
        /// Return the enumerable as a IList of T, copying if required. Avoid mutating the return value.
        /// </summary>
        public static IList<T> AsIList<T>(this IEnumerable<T> enumerable)
        {
            Debug.Assert(enumerable != null);

            var list = enumerable as IList<T>;
            if (list != null)
            {
                return list;
            }
            return new List<T>(enumerable);
        }

        /// <summary>
        /// Return the enumerable as a List of T, copying if required. Optimized for common case where it is an List of
        /// T or a ListWrapperCollection of T. Avoid mutating the return value.
        /// </summary>
        public static List<T> AsList<T>(this IEnumerable<T> enumerable)
        {
            Debug.Assert(enumerable != null);

            List<T> list = enumerable as List<T>;
            if (list != null)
            {
                return list;
            }
            ListWrapperCollection<T> listWrapper = enumerable as ListWrapperCollection<T>;
            if (listWrapper != null)
            {
                return listWrapper.ItemsList;
            }
            return new List<T>(enumerable);
        }

        /// <summary>
        /// Remove values from the list starting at the index start.
        /// </summary>
        public static void RemoveFrom<T>(this List<T> list, int start)
        {
            Debug.Assert(list != null);
            Debug.Assert(start >= 0 && start <= list.Count);

            list.RemoveRange(start, list.Count - start);
        }

        /// <summary>
        /// Return the only value from list, the type's default value if empty, or call the errorAction for 2 or more.
        /// </summary>
        public static T SingleDefaultOrError<T, TArg1>(this IList<T> list, Action<TArg1> errorAction, TArg1 errorArg1)
        {
            Debug.Assert(list != null);
            Debug.Assert(errorAction != null);

            switch (list.Count)
            {
                case 0:
                    return default(T);

                case 1:
                    var value = list[0];
                    return value;

                default:
                    errorAction(errorArg1);
                    return default(T);
            }
        }

        /// <summary>
        /// Returns a single value in list matching type TMatch if there is only one, null if there are none of type
        /// TMatch or calls the errorAction with errorArg1 if there is more than one.
        /// </summary>
        public static TMatch SingleOfTypeDefaultOrError<TInput, TMatch, TArg1>(
            this IList<TInput> list,
            Action<TArg1> errorAction,
            TArg1 errorArg1) where TMatch : class
        {
            Debug.Assert(list != null);
            Debug.Assert(errorAction != null);

            TMatch result = null;
            for (var i = 0; i < list.Count; i++)
            {
                var typedValue = list[i] as TMatch;
                if (typedValue != null)
                {
                    if (result == null)
                    {
                        result = typedValue;
                    }
                    else
                    {
                        errorAction(errorArg1);
                        return null;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Convert an ICollection to an array, removing null values. Fast path for case where there are no null
        /// values.
        /// </summary>
        public static T[] ToArrayWithoutNulls<T>(this ICollection<T> collection) where T : class
        {
            Debug.Assert(collection != null);

            var result = new T[collection.Count];
            var count = 0;
            foreach (var value in collection)
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
                var trimmedResult = new T[count];
                Array.Copy(result, trimmedResult, count);
                return trimmedResult;
            }
        }

        /// <summary>
        /// Convert the array to a Dictionary using the keySelector to extract keys from values and the specified
        /// comparer. Optimized for array input.
        /// </summary>
        public static Dictionary<TKey, TValue> ToDictionaryFast<TKey, TValue>(
            this TValue[] array,
            Func<TValue, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            Debug.Assert(array != null);
            Debug.Assert(keySelector != null);

            var dictionary = new Dictionary<TKey, TValue>(array.Length, comparer);
            for (var i = 0; i < array.Length; i++)
            {
                var value = array[i];
                dictionary.Add(keySelector(value), value);
            }
            return dictionary;
        }

        /// <summary>
        /// Convert the list to a Dictionary using the keySelector to extract keys from values and the specified
        /// comparer. Optimized for IList of T input with fast path for array.
        /// </summary>
        public static Dictionary<TKey, TValue> ToDictionaryFast<TKey, TValue>(
            this IList<TValue> list,
            Func<TValue, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            Debug.Assert(list != null);
            Debug.Assert(keySelector != null);

            var array = list as TValue[];
            if (array != null)
            {
                return ToDictionaryFast(array, keySelector, comparer);
            }
            return ToDictionaryFastNoCheck(list, keySelector, comparer);
        }

        /// <summary>
        /// Convert the enumerable to a Dictionary using the keySelector to extract keys from values and the specified
        /// comparer. Fast paths for array and IList of T.
        /// </summary>
        public static Dictionary<TKey, TValue> ToDictionaryFast<TKey, TValue>(
            this IEnumerable<TValue> enumerable,
            Func<TValue, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            Debug.Assert(enumerable != null);
            Debug.Assert(keySelector != null);

            var array = enumerable as TValue[];
            if (array != null)
            {
                return ToDictionaryFast(array, keySelector, comparer);
            }
            var list = enumerable as IList<TValue>;
            if (list != null)
            {
                return ToDictionaryFastNoCheck(list, keySelector, comparer);
            }
            var dictionary = new Dictionary<TKey, TValue>(comparer);
            foreach (var value in enumerable)
            {
                dictionary.Add(keySelector(value), value);
            }
            return dictionary;
        }

        /// <summary>
        /// Convert the list to a Dictionary using the keySelector to extract keys from values and the specified
        /// comparer. Optimized for IList of T input. No checking for other types.
        /// </summary>
        private static Dictionary<TKey, TValue> ToDictionaryFastNoCheck<TKey, TValue>(
            IList<TValue> list,
            Func<TValue, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            Debug.Assert(list != null);
            Debug.Assert(keySelector != null);

            var listCount = list.Count;
            var dictionary = new Dictionary<TKey, TValue>(listCount, comparer);
            for (var i = 0; i < listCount; i++)
            {
                var value = list[i];
                dictionary.Add(keySelector(value), value);
            }
            return dictionary;
        }
    }
}
#endif