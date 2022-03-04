// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// An entry in a <see cref="ValidationStateDictionary"/>. Records state information to override the default
    /// behavior of validation for an object.
    /// </summary>
    public class ValidationStateEntry
    {
        /// <summary>
        /// Gets or sets the model prefix associated with the entry.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ModelMetadata"/> associated with the entry.
        /// </summary>
        public ModelMetadata Metadata { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the associated model object should be validated.
        /// </summary>
        public bool SuppressValidation { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="IValidationStrategy"/> for enumerating child entries of the associated
        /// model object.
        /// </summary>
        public IValidationStrategy Strategy { get; set; }
    }
}
