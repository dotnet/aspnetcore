// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Aggregate of <see cref="IClientModelValidatorProvider"/>s that delegates to its underlying providers.
    /// </summary>
    public class CompositeClientModelValidatorProvider : IClientModelValidatorProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompositeClientModelValidatorProvider"/>.
        /// </summary>
        /// <param name="providers">
        /// A collection of <see cref="IClientModelValidatorProvider"/> instances.
        /// </param>
        public CompositeClientModelValidatorProvider([NotNull] IEnumerable<IClientModelValidatorProvider> providers)
        {
            ValidatorProviders = new List<IClientModelValidatorProvider>(providers);
        }

        /// <summary>
        /// Gets a list of <see cref="IClientModelValidatorProvider"/> instances.
        /// </summary>
        public IReadOnlyList<IClientModelValidatorProvider> ValidatorProviders { get; }

        /// <inheritdoc />
        public void GetValidators(ClientValidatorProviderContext context)
        {
            foreach (var validatorProvider in ValidatorProviders)
            {
                validatorProvider.GetValidators(context);
            }
        }
    }
}