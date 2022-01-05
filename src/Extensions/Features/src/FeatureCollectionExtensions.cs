// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Extension methods for getting feature from <see cref="IFeatureCollection"/>
/// </summary>
public static class FeatureCollectionExtensions
{
    /// <summary>
    /// Get feature of type <typeparamref name="TFeature"/> from the <see cref="IFeatureCollection"/>.
    /// Exception of type <see cref="InvalidOperationException"/> thrown when asked for unregistered feature type
    /// </summary>
    /// <param name="featureCollection"></param>
    /// <returns>Feature object type</returns>
    public static TFeature GetRequiredFeature<TFeature>(this IFeatureCollection featureCollection)
        where TFeature : notnull
    {
        if (featureCollection is null)
        {
            throw new ArgumentNullException(nameof(featureCollection));
        }

        return featureCollection.Get<TFeature>() ?? throw new InvalidOperationException($"{nameof(TFeature)} is not available");
    }

    /// <summary>
    /// Get feature object of provided type from the <see cref="IFeatureCollection"/>.
    /// Exception of type <see cref="InvalidOperationException"/> thrown when asked for unregistered feature type
    /// </summary>
    /// <param name="featureCollection">feature collection</param>
    /// <param name="featureType">type of feature</param>
    /// <returns>feature object</returns>
    public static object GetRequiredFeature(this IFeatureCollection featureCollection, Type featureType)
    {
        if (featureCollection == null)
        {
            throw new ArgumentNullException(nameof(featureCollection));
        }

        if (featureType == null)
        {
            throw new ArgumentNullException(nameof(featureType));
        }

        return featureCollection[featureType] ?? throw new InvalidOperationException($"{nameof(featureType)} is not available");
    }
}
