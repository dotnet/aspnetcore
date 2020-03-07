// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    internal class ArrayPoolBufferSource : ICharBufferSource
    {
        private readonly ArrayPool<char> _pool;

        public ArrayPoolBufferSource(ArrayPool<char> pool)
        {
            _pool = pool;
        }

        public char[] Rent(int bufferSize) => _pool.Rent(bufferSize);

        public void Return(char[] buffer) => _pool.Return(buffer);
    }
}
