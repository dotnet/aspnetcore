// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a NotAcceptable (406) response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class NotAcceptableObjectResult : ObjectResult
    {
        private const int DefaultStatusCode = StatusCodes.Status406NotAcceptable;

        /// <summary>
        /// Creates a new <see cref="NotAcceptableObjectResult"/> instance.
        /// </summary>
        /// <param name="error">Contains the errors to be returned to the client.</param>
        public NotAcceptableObjectResult([ActionResultObjectValue] object error)
            : base(error)
        {
            StatusCode = DefaultStatusCode;
        }

        /// <summary>
        /// Creates a new <see cref="NotAcceptableObjectResult"/> instance.
        /// </summary>
        /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
        public NotAcceptableObjectResult([ActionResultObjectValue] ModelStateDictionary modelState)
            : base(new SerializableError(modelState))
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            StatusCode = DefaultStatusCode;
        }
    }
}
