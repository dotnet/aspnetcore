// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a Bad Request (400) response.
    /// </summary>
    public class BadRequestObjectResult : ObjectResult
    {
        /// <summary>
        /// Creates a new <see cref="BadRequestObjectResult"/> instance.
        /// </summary>
        /// <param name="error">Contains the errors to be returned to the client.</param>
        public BadRequestObjectResult(object error)
            : base(error)
        {
            StatusCode = StatusCodes.Status400BadRequest;
        }

        /// <summary>
        /// Creates a new <see cref="BadRequestObjectResult"/> instance.
        /// </summary>
        /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
        public BadRequestObjectResult([NotNull] ModelStateDictionary modelState)
            : base(new SerializableError(modelState))
        {
            StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}