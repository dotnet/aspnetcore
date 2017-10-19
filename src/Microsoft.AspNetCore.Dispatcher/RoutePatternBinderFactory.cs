// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Dispatcher.Patterns;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class RoutePatternBinderFactory
    {
        private readonly UrlEncoder _encoder;
        private readonly ObjectPool<UriBuildingContext> _pool;

        public RoutePatternBinderFactory(UrlEncoder encoder, ObjectPoolProvider objectPoolProvider)
        {
            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            if (objectPoolProvider == null)
            {
                throw new ArgumentNullException(nameof(objectPoolProvider));
            }

            _encoder = encoder;
            _pool = objectPoolProvider.Create(new UriBuilderContextPooledObjectPolicy());
        }

        public RoutePatternBinder Create(string pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            return Create(RoutePattern.Parse(pattern), new DispatcherValueCollection());
        }

        public RoutePatternBinder Create(string pattern, DispatcherValueCollection defaults)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (defaults == null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            return Create(RoutePattern.Parse(pattern), defaults);
        }

        public RoutePatternBinder Create(RoutePattern pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            return Create(pattern, new DispatcherValueCollection());
        }

        public RoutePatternBinder Create(RoutePattern pattern, DispatcherValueCollection defaults)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (defaults == null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            return new RoutePatternBinder(_encoder, _pool, pattern, defaults);
        }

        private class UriBuilderContextPooledObjectPolicy : IPooledObjectPolicy<UriBuildingContext>
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
}
