// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.ObjectPool
{
    public class LeakTrackingObjectPoolProvider : ObjectPoolProvider
    {
        private readonly ObjectPoolProvider _inner;

        public LeakTrackingObjectPoolProvider(ObjectPoolProvider inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            _inner = inner;
        }

        public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
        {
            var inner = _inner.Create<T>(policy);
            return new LeakTrackingObjectPool<T>(inner);
        }
    }
}
