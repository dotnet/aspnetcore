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
    /// Get feature of type <typeparamref name="TFeature"/> from the <see cref="IFeatureCollection"/>
    /// </summary>
    /// <param name="featureCollection"></param>
    /// <returns>Feature object type</returns>
    /// <exception cref="InvalidOperationException">Thrown when asked for unregistered feature type</exception>
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
    /// Get feature object of provided type from the <see cref="IFeatureCollection"/>
    /// </summary>
    /// <param name="featureCollection">feature collection</param>
    /// <param name="featureType">type of feature</param>
    /// <returns>feature object</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="featureCollection"/> is null </exception>
    /// <exception cref="InvalidOperationException">Thrown when asked for unregistered feature type</exception>
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

        var feature = featureCollection[featureType];

        if (feature == null)
        {
            throw new InvalidOperationException($"{nameof(featureType)} is not available");
        }

        return feature;
    }
}
