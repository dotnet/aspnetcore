// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Provides methods to validate an object graph.
    /// </summary>
    public interface IObjectModelValidator
    {
        /// <summary>
        /// Validates the given model in <see cref="ModelValidationContext.ModelExplorer"/>.
        /// </summary>
        /// <param name="validationContext">The <see cref="ModelValidationContext"/> associated with the current call.
        /// </param>
        void Validate(ModelValidationContext validationContext);
    }
}
