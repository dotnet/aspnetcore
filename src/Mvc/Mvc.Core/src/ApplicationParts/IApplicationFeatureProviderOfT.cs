// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// A provider for a given <typeparamref name="TFeature"/> feature.
/// </summary>
/// <typeparam name="TFeature">The type of the feature.</typeparam>
public interface IApplicationFeatureProvider<TFeature> : IApplicationFeatureProvider
{
    /// <summary>
    /// Updates the <paramref name="feature"/> instance.
    /// </summary>
    /// <param name="parts">The list of <see cref="ApplicationPart"/> instances in the application.
    /// </param>
    /// <param name="feature">The feature instance to populate.</param>
    /// <remarks>
    /// <see cref="ApplicationPart"/> instances in <paramref name="parts"/> appear in the same ordered sequence they
    /// are stored in <see cref="ApplicationPartManager.ApplicationParts"/>. This ordering may be used by the feature
    /// provider to make precedence decisions.
    /// </remarks>
    void PopulateFeature(IEnumerable<ApplicationPart> parts, TFeature feature);
}
