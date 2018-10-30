// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.Extensions.ObjectPool
{
    public static class ObjectPoolProviderExtensions
    {
        public static ObjectPool<StringBuilder> CreateStringBuilderPool(this ObjectPoolProvider provider)
        {
            return provider.Create<StringBuilder>(new StringBuilderPooledObjectPolicy());
        }

        public static ObjectPool<StringBuilder> CreateStringBuilderPool(
            this ObjectPoolProvider provider,
            int initialCapacity,
            int maximumRetainedCapacity)
        {
            var policy = new StringBuilderPooledObjectPolicy()
            {
                InitialCapacity = initialCapacity,
                MaximumRetainedCapacity = maximumRetainedCapacity,
            };

            return provider.Create<StringBuilder>(policy);
        }
    }
}
