// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.ObjectPool
{
    public abstract class ObjectPoolProvider
    {
        public ObjectPool<T> Create<T>() where T : class, new()
        {
            return Create<T>(new DefaultPooledObjectPolicy<T>());
        }

        public abstract ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class;
    }
}
