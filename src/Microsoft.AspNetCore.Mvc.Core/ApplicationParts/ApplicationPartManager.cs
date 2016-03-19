// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Manages the parts and features of an MVC application.
    /// </summary>
    public class ApplicationPartManager
    {
        /// <summary>
        /// Gets the list of <see cref="IApplicationFeatureProvider"/>s.
        /// </summary>
        public IList<IApplicationFeatureProvider> FeatureProviders { get; } =
            new List<IApplicationFeatureProvider>();

        /// <summary>
        /// Gets the list of <see cref="ApplicationPart"/>s.
        /// </summary>
        public IList<ApplicationPart> ApplicationParts { get; } =
            new List<ApplicationPart>();

        /// <summary>
        /// Populates the given <paramref name="feature"/> using the list of
        /// <see cref="IApplicationFeatureProvider{TFeature}"/>s configured on the
        /// <see cref="ApplicationPartManager"/>.
        /// </summary>
        /// <typeparam name="TFeature">The type of the feature.</typeparam>
        /// <param name="feature">The feature instance to populate.</param>
        public void PopulateFeature<TFeature>(TFeature feature)
        {
            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            foreach (var provider in FeatureProviders.OfType<IApplicationFeatureProvider<TFeature>>())
            {
                provider.PopulateFeature(ApplicationParts, feature);
            }
        }
    }
}
