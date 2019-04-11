// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class UriBuilderContextPooledObjectPolicy : IPooledObjectPolicy<UriBuildingContext>
    {
        public UriBuildingContext Create()
        {
            return new UriBuildingContext(UrlEncoder.Default);
        }

        public bool Return(UriBuildingContext obj)
        {
            obj.Clear();
            return true;
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
