// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// A cached reference to a feature.
    /// </summary>
    /// <typeparam name="T">The feature type.</typeparam>
    public struct FeatureReference<T>
    {
        private T? _feature;
        private int _revision;

        private FeatureReference(T? feature, int revision)
        {
            _feature = feature;
            _revision = revision;
        }

        /// <summary>
        /// Gets the default <see cref="FeatureReference{T}"/>.
        /// </summary>
        public static readonly FeatureReference<T> Default = new FeatureReference<T>(default(T), -1);

        /// <summary>
        /// Gets the feature of type <typeparamref name="T"/> from <paramref name="features"/>.
        /// </summary>
        /// <param name="features">The <see cref="IFeatureCollection"/>.</param>
        /// <returns>The feature.</returns>
        public T? Fetch(IFeatureCollection features)
        {
            if (_revision == features.Revision)
            {
                return _feature;
            }
            _feature = (T?)features[typeof(T)];
            _revision = features.Revision;
            return _feature;
        }

        /// <summary>
        /// Updates the reference to the feature.
        /// </summary>
        /// <param name="features">The <see cref="IFeatureCollection"/> to update.</param>
        /// <param name="feature">The instance of the feature.</param>
        /// <returns>A reference to <paramref name="feature"/> after the operation has completed.</returns>
        public T Update(IFeatureCollection features, T feature)
        {
            features[typeof(T)] = feature;
            _feature = feature;
            _revision = features.Revision;
            return feature;
        }
    }
}
