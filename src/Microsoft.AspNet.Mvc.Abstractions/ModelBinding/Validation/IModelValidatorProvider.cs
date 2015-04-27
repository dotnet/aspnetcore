// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Provides validators for a model value.
    /// </summary>
    public interface IModelValidatorProvider
    {
        /// <summary>
        /// Gets the validators for <see cref="ModelValidatorProviderContext.ModelMetadata"/>.
        /// </summary>
        /// <param name="context">The <see cref="ModelValidatorProviderContext"/>.</param>
        /// <remarks>
        /// Implementations should add <see cref="IModelValidator"/> instances to
        /// <see cref="ModelValidatorProviderContext.Validators"/>.
        /// </remarks>
        void GetValidators(ModelValidatorProviderContext context);
    }
}
