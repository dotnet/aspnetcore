// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.ObjectPool
{
    public abstract class ObjectPool<T> where T : class
    {
        public abstract T Get();

        public abstract void Return(T obj);
    }

    public static class ObjectPool
    {
        public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy = null) where T : class, new()
        {
            var provider = new DefaultObjectPoolProvider();
            return provider.Create(policy ?? new DefaultPooledObjectPolicy<T>());
        }
    }
}
