// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a Unprocessable Entity (422) response.
    /// </summary>
    public class UnprocessableEntityObjectResult : ObjectResult
    {
        /// <summary>
        /// Creates a new <see cref="UnprocessableEntityObjectResult"/> instance.
        /// </summary>
        /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
        public UnprocessableEntityObjectResult(ModelStateDictionary modelState)
            : this(new SerializableError(modelState))
        {
        }

        /// <summary>
        /// Creates a new <see cref="UnprocessableEntityObjectResult"/> instance.
        /// </summary>
        /// <param name="error">Contains errors to be returned to the client.</param>
        public UnprocessableEntityObjectResult(object error)
            : base(error)
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity;
        }
    }
}
