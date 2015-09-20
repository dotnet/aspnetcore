// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

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
            [NotNull] HttpContext httpContext,
            [NotNull] string modelName,
            [NotNull] ModelStateDictionary modelState,
            [NotNull] Type modelType)
        {
            HttpContext = httpContext;
            ModelName = modelName;
            ModelState = modelState;
            ModelType = modelType;
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
        /// Gets the expected <see cref="Type"/> of the model represented by the request body.
        /// </summary>
        public Type ModelType { get; }
    }
}
