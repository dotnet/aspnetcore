// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A context object for <see cref="IModelValidator"/>.
    /// </summary>
    public class ModelValidationContext
    {
        /// <summary>
        /// Gets or sets the model object.
        /// </summary>
        public object Model { get; set; }

        /// <summary>
        /// Gets or sets the model container object.
        /// </summary>
        public object Container { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ModelMetadata"/> associated with <see cref="Model"/>.
        /// </summary>
        public ModelMetadata Metadata { get; set; }
    }
}
