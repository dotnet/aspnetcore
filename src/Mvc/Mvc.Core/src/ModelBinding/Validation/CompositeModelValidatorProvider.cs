// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Aggregate of <see cref="IModelValidatorProvider"/>s that delegates to its underlying providers.
    /// </summary>
    public class CompositeModelValidatorProvider : IModelValidatorProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompositeModelValidatorProvider"/>.
        /// </summary>
        /// <param name="providers">
        /// A collection of <see cref="IModelValidatorProvider"/> instances.
        /// </param>
        public CompositeModelValidatorProvider(IList<IModelValidatorProvider> providers)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            ValidatorProviders = providers;
        }

        /// <summary>
        /// Gets the list of <see cref="IModelValidatorProvider"/> instances.
        /// </summary>
        public IList<IModelValidatorProvider> ValidatorProviders { get; }

        /// <inheritdoc />
        public void CreateValidators(ModelValidatorProviderContext context)
        {
            // Perf: Avoid allocations
            for (var i = 0; i < ValidatorProviders.Count; i++)
            {
                ValidatorProviders[i].CreateValidators(context);
            }
        }
    }
}
