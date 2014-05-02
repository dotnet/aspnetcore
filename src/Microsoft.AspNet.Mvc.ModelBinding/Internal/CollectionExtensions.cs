// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
