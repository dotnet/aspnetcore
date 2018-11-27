// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    internal class JsonArrayPool<T> : IArrayPool<T>
    {
        private readonly ArrayPool<T> _inner;

        public JsonArrayPool(ArrayPool<T> inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            _inner = inner;
        }

        public T[] Rent(int minimumLength)
        {
            return _inner.Rent(minimumLength);
        }

        public void Return(T[] array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            _inner.Return(array);
        }
    }
}
