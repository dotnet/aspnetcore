// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.Net.Http.Headers
{
    // List<T> allows 'null' values to be added. This is not what we want so we use a custom Collection<T> derived
    // type to throw if 'null' gets added. Collection<T> internally uses List<T> which comes at some cost. In addition
    // Collection<T>.Add() calls List<T>.InsertItem() which is an O(n) operation (compared to O(1) for List<T>.Add()).
    // This type is only used for very small collections (1-2 items) to keep the impact of using Collection<T> small.
    internal class ObjectCollection<T> : Collection<T> where T : class
    {
        private static readonly Action<T> DefaultValidator = CheckNotNull;

        private Action<T> _validator;

        public ObjectCollection()
            : this(DefaultValidator)
        {
        }

        public ObjectCollection(Action<T> validator)
        {
            _validator = validator;
        }

        protected override void InsertItem(int index, T item)
        {
            if (_validator != null)
            {
                _validator(item);
            }
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item)
        {
            if (_validator != null)
            {
                _validator(item);
            }
            base.SetItem(index, item);
        }

        private static void CheckNotNull(T item)
        {
            // null values cannot be added to the collection.
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
        }
    }
}