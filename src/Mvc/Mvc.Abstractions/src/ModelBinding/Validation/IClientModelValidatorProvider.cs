// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Provides a collection of <see cref="IClientModelValidator"/>s.
    /// </summary>
    public interface IClientModelValidatorProvider
    {
        /// <summary>
        /// Creates set of <see cref="IClientModelValidator"/>s by updating
        /// <see cref="ClientValidatorItem.Validator"/> in <see cref="ClientValidatorProviderContext.Results"/>.
        /// </summary>
        /// <param name="context">The <see cref="ClientModelValidationContext"/> associated with this call.</param>
        void CreateValidators(ClientValidatorProviderContext context);
    }
}
