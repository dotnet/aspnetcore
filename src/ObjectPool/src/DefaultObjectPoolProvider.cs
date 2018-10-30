// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.ObjectPool
{
    public class DefaultObjectPoolProvider : ObjectPoolProvider
    {
        public int MaximumRetained { get; set; } = Environment.ProcessorCount * 2;

        public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        {
            return new DefaultObjectPool<T>(policy, MaximumRetained);
        }
    }
}
