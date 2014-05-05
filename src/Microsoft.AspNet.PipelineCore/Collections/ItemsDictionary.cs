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

using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.PipelineCore
{
    public class ItemsDictionary : IDictionary<object, object>
    {
        public ItemsDictionary()
            : this(new Dictionary<object, object>())
        {
        }

        public ItemsDictionary(IDictionary<object, object> items)
        {
            Items = items;
        }

        public IDictionary<object, object> Items { get; private set; }

        // Replace the indexer with one that returns null for missing values
        object IDictionary<object, object>.this[object key]
        {
            get
            {
                object value;
                if (Items.TryGetValue(key, out value))
                {
                    return value;
                }
                return null;
            }
            set { Items[key] = value; }
        }

        void IDictionary<object, object>.Add(object key, object value)
        {
            Items.Add(key, value);
        }

        bool IDictionary<object, object>.ContainsKey(object key)
        {
            return Items.ContainsKey(key);
        }

        ICollection<object> IDictionary<object, object>.Keys
        {
            get { return Items.Keys; }
        }

        bool IDictionary<object, object>.Remove(object key)
        {
            return Items.Remove(key);
        }

        bool IDictionary<object, object>.TryGetValue(object key, out object value)
        {
            return Items.TryGetValue(key, out value);
        }

        ICollection<object> IDictionary<object, object>.Values
        {
            get { return Items.Values; }
        }

        void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> item)
        {
            Items.Add(item);
        }

        void ICollection<KeyValuePair<object, object>>.Clear()
        {
            Items.Clear();
        }

        bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item)
        {
            return Items.Contains(item);
        }

        void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<object, object>>.Count
        {
            get { return Items.Count; }
        }

        bool ICollection<KeyValuePair<object, object>>.IsReadOnly
        {
            get { return Items.IsReadOnly; }
        }

        bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item)
        {
            return Items.Remove(item);
        }

        IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}