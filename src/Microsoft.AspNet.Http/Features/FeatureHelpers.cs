// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Http.Features
{
    internal sealed class FeatureHelpers
    {
        public static T GetAndCache<T>(
            IFeatureCache cache, 
            IFeatureCollection features, 
            ref T cachedObject)
        {
            cache.CheckFeaturesRevision();

            T obj = cachedObject;
            if (obj == null)
            {
                obj = features.Get<T>();
                cachedObject = obj;
            }
            return obj;
        }

        public static T GetOrCreate<T>(
            IFeatureCollection features,
            Func<T> factory)
        {
            T obj = features.Get<T>();
            if (obj == null)
            {
                obj = factory();
                features.Set(obj);
            }

            return obj;
        }


        public static T GetOrCreateAndCache<T>(
            IFeatureCache cache, 
            IFeatureCollection features,
            Func<T> factory,
            ref T cachedObject)
        {
            cache.CheckFeaturesRevision();

            T obj = cachedObject;
            if (obj == null)
            {
                obj = features.Get<T>();
                if (obj == null)
                {
                    obj = factory();
                    cachedObject = obj;
                    features.Set(obj);
                }
            }
            return obj;
        }
        
        public static T GetOrCreateAndCache<T>(
            IFeatureCache cache,
            IFeatureCollection features, 
            Func<IFeatureCollection, T> factory,
            ref T cachedObject)
        {
            cache.CheckFeaturesRevision();

            T obj = cachedObject;
            if (obj == null)
            {
                obj = features.Get<T>();
                if (obj == null)
                {
                    obj = factory(features);
                    cachedObject = obj;
                    features.Set(obj);
                }
            }
            return obj;
        }

        public static T GetOrCreateAndCache<T>(
            IFeatureCache cache,
            IFeatureCollection features,
            HttpRequest request,
            Func<HttpRequest, T> factory,
            ref T cachedObject)
        {
            cache.CheckFeaturesRevision();

            T obj = cachedObject;
            if (obj == null)
            {
                obj = features.Get<T>();
                if (obj == null)
                {
                    obj = factory(request);
                    cachedObject = obj;
                    features.Set(obj);
                }
            }
            return obj;
        }
    }
}
