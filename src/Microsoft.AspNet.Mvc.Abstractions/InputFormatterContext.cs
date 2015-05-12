// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
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
        /// <param name="modelState">
        /// The <see cref="ModelStateDictionary"/> for recording errors.
        /// </param>
        /// <param name="modelType">
        /// The <see cref="Type"/> of the model to deserialize.
        /// </param>
        public InputFormatterContext(
            [NotNull] HttpContext httpContext,
            [NotNull] ModelStateDictionary modelState,
            [NotNull] Type modelType)
        {
            HttpContext = httpContext;
            ModelState = modelState;
            ModelType = modelType;
        }

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/> associated with the current operation.
        /// </summary>
        public HttpContext HttpContext { get; private set; }

        /// <summary>
        /// Gets the <see cref="ModelStateDictionary"/> associated with the current operation.
        /// </summary>
        public ModelStateDictionary ModelState { get; }

        /// <summary>
        /// Gets the expected <see cref="Type"/> of the model represented by the request body.
        /// </summary>
        public Type ModelType { get; private set; }
    }
}
