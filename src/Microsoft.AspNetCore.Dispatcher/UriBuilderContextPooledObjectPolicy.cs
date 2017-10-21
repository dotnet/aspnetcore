// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class UriBuilderContextPooledObjectPolicy : IPooledObjectPolicy<UriBuildingContext>
    {
        public UriBuildingContext Create()
        {
            return new UriBuildingContext();
        }

        public bool Return(UriBuildingContext obj)
        {
            obj.Clear();
            return true;
        }
    }
}
