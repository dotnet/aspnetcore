// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Http.Features
{
    public static class FeatureCollectionExtensions
    {
        /// <summary>
        /// Retrieves the requested feature from the collection.
        /// </summary>
        /// <typeparam name="TFeature">The feature key.</typeparam>
        /// <param name="features">The collection.</param>
        /// <returns>The requested feature, or null if it is not present.</returns>
        public static TFeature Get<TFeature>(this IFeatureCollection features)
        {
            return (TFeature)features[typeof(TFeature)];
        }

        /// <summary>
        /// Sets the given feature in the collection.
        /// </summary>
        /// <typeparam name="TFeature">The feature key.</typeparam>
        /// <param name="features">The collection.</param>
        /// <param name="instance">The feature value.</param>
        public static void Set<TFeature>(this IFeatureCollection features, TFeature instance)
        {
            features[typeof(TFeature)] = instance;
        }
    }
}
