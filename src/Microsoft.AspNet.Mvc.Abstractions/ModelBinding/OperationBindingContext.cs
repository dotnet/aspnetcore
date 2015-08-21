// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A context that contains information specific to the current request and the action whose parameters
    /// are being model bound.
    /// </summary>
    public class OperationBindingContext
    {
        /// <summary>
        /// Gets or sets the <see cref="HttpContext"/> for the current request.
        /// </summary>
        public HttpContext HttpContext { get; set; }

        /// <summary>
        /// Gets or sets the set of <see cref="IInputFormatter"/> instances associated with this context.
        /// </summary>
        public IList<IInputFormatter> InputFormatters { get; set; }

        /// <summary>
        /// Gets unaltered value provider collection.
        /// Value providers can be filtered by specific model binders.
        /// </summary>
        public IValueProvider ValueProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IModelBinder"/> associated with this context.
        /// </summary>
        public IModelBinder ModelBinder { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IModelMetadataProvider"/> associated with this context.
        /// </summary>
        public IModelMetadataProvider MetadataProvider { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IModelValidatorProvider"/> instance used for model validation with this
        /// context.
        /// </summary>
        public IModelValidatorProvider ValidatorProvider { get; set; }
    }
}
