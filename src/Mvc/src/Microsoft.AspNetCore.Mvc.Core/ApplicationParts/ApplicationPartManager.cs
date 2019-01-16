// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        /// Gets the list of <see cref="ApplicationPart"/> instances.
        /// <para>
        /// Instances in this collection are stored in precedence order. An <see cref="ApplicationPart"/> that appears
        /// earlier in the list has a higher precedence.
        /// An <see cref="IApplicationFeatureProvider"/> may choose to use this an interface as a way to resolve conflicts when
        /// multiple <see cref="ApplicationPart"/> instances resolve equivalent feature values.
        /// </para>
        /// </summary>
        public IList<ApplicationPart> ApplicationParts { get; } = new List<ApplicationPart>();

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

        internal void PopulateDefaultParts(string entryAssemblyName)
        {
            var entryAssembly = Assembly.Load(new AssemblyName(entryAssemblyName));
            var assembliesProvider = new ApplicationAssembliesProvider();
            var applicationAssemblies = assembliesProvider.ResolveAssemblies(entryAssembly);

            foreach (var assembly in applicationAssemblies)
            {
                var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
                foreach (var part in partFactory.GetApplicationParts(assembly))
                {
                    ApplicationParts.Add(part);
                }
            }
        }
    }
}
