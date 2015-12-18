// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNet.Routing.Internal
{
    public class UriBuilderContextPooledObjectPolicy : IPooledObjectPolicy<UriBuildingContext>
    {
        private readonly UrlEncoder _encoder;

        public UriBuilderContextPooledObjectPolicy(UrlEncoder encoder)
        {
            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            _encoder = encoder;
        }

        public UriBuildingContext Create()
        {
            return new UriBuildingContext(_encoder);
        }

        public bool Return(UriBuildingContext obj)
        {
            obj.Clear();
            return true;
        }
    }
}
