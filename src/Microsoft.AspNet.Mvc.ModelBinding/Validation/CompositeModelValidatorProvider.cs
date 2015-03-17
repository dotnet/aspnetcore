// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Default implementation for <see cref="ICompositeModelValidatorProvider"/>.
    /// </summary>
    public class CompositeModelValidatorProvider : ICompositeModelValidatorProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompositeModelValidatorProvider"/>.
        /// </summary>
        /// <param name="providers">
        /// A collection of <see cref="IModelValidatorProvider"/> instances.
        /// </param>
        public CompositeModelValidatorProvider([NotNull] IEnumerable<IModelValidatorProvider> providers)
        {
            ValidatorProviders = new List<IModelValidatorProvider>(providers);
        }

        public IReadOnlyList<IModelValidatorProvider> ValidatorProviders { get; }

        public void GetValidators(ModelValidatorProviderContext context)
        {
            foreach (var validatorProvider in ValidatorProviders)
            {
                validatorProvider.GetValidators(context);
            }
        }
    }
}