// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// A context object used by an input formatter for deserializing the request body into an object.
    /// </summary>
    public class InputFormatterContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="InputFormatterContext"/>.
        /// </summary>
        /// <param name="httpContext">
        /// The <see cref="Http.HttpContext"/> for the current operation.
        /// </param>
        /// <param name="modelName">The name of the model.</param>
        /// <param name="modelState">
        /// The <see cref="ModelStateDictionary"/> for recording errors.
        /// </param>
        /// <param name="modelType">
        /// The <see cref="Type"/> of the model to deserialize.
        /// </param>
        public InputFormatterContext(
            HttpContext httpContext,
            string modelName,
            ModelStateDictionary modelState,
            ModelMetadata metadata)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            HttpContext = httpContext;
            ModelName = modelName;
            ModelState = modelState;
            Metadata = metadata;
            ModelType = metadata.ModelType;
        }

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/> associated with the current operation.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// Gets the name of the model. Used as the key or key prefix for errors added to <see cref="ModelState"/>.
        /// </summary>
        public string ModelName { get; }

        /// <summary>
        /// Gets the <see cref="ModelStateDictionary"/> associated with the current operation.
        /// </summary>
        public ModelStateDictionary ModelState { get; }

        /// <summary>
        /// Gets the requested <see cref="ModelMetadata"/> of the request body deserialization.
        /// </summary>
        public ModelMetadata Metadata { get; }

        /// <summary>
        /// Gets the requested <see cref="Type"/> of the request body deserialization.
        /// </summary>
        public Type ModelType { get; }
    }
}
