// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    internal class TestArrayPool<T> : ArrayPool<T>
    {
        public override T[] Rent(int minimumLength)
        {
            return new T[minimumLength];
        }

        public List<T[]> ReturnedBuffers = new List<T[]>();

        public override void Return(T[] array, bool clearArray = false)
        {
            ReturnedBuffers.Add(array);
        }
    }
}
