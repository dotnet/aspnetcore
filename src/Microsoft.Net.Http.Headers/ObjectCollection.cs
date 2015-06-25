// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Net.Http.Headers
{
    // List<T> allows 'null' values to be added. This is not what we want so we use a custom Collection<T> derived
    // type to throw if 'null' gets added. Collection<T> internally uses List<T> which comes at some cost. In addition
    // Collection<T>.Add() calls List<T>.InsertItem() which is an O(n) operation (compared to O(1) for List<T>.Add()).
    // This type is only used for very small collections (1-2 items) to keep the impact of using Collection<T> small.
    internal class ObjectCollection<T> : ICollection<T> where T : class
    {
        internal static readonly Action<T> DefaultValidator = CheckNotNull;
        internal static readonly ObjectCollection<T> EmptyReadOnlyCollection
            = new ObjectCollection<T>(DefaultValidator, isReadOnly: true);

        private readonly Collection<T> _collection = new Collection<T>();
        private readonly Action<T> _validator;
        private readonly bool _isReadOnly;

        public ObjectCollection()
            : this(DefaultValidator)
        {
        }

        public ObjectCollection(Action<T> validator, bool isReadOnly = false)
        {
            _validator = validator;
            _isReadOnly = isReadOnly;
        }

        public ObjectCollection(IEnumerable<T> other, bool isReadOnly = false)
        {
            _validator = DefaultValidator;
            foreach (T item in other)
            {
                Add(item);
            }
            _isReadOnly = isReadOnly;
        }

        public int Count
        {
            get { return _collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
        }

        public void Add(T item)
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            _validator(item);
            _collection.Add(item);
        }

        public bool Remove(T item)
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            return _collection.Remove(item);
        }

        public void Clear()
        {
            HeaderUtilities.ThrowIfReadOnly(IsReadOnly);
            _collection.Clear();
        }

        public bool Contains(T item) => _collection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

        private static void CheckNotNull(T item)
        {
            // null values cannot be added to the collection.
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
        }
    }
}