// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// A provider for a given <typeparamref name="TFeature"/> feature.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    public interface IApplicationFeatureProvider<TFeature> : IApplicationFeatureProvider
    {
        /// <summary>
        /// Updates the <paramref name="feature"/> instance.
        /// </summary>
        /// <param name="parts">The list of <see cref="ApplicationPart"/>s of the
        /// application.
        /// </param>
        /// <param name="feature">The feature instance to populate.</param>
        void PopulateFeature(IEnumerable<ApplicationPart> parts, TFeature feature);
    }
}
