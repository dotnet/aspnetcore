// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Validation metadata details for a <see cref="ModelMetadata"/>.
    /// </summary>
    public class ValidationMetadata
    {
        /// <summary>
        /// Gets a list of metadata items for validators.
        /// </summary>
        /// <remarks>
        /// <see cref="IValidationMetadataProvider"/> implementations should store metadata items
        /// in this list, to be consumed later by an <see cref="Validation.IModelValidatorProvider"/>.
        /// </remarks>
        public IList<object> ValidatorMetadata { get; } = new List<object>();
    }
}