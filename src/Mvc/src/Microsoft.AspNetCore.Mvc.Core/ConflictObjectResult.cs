// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a Conflict (409) response.
    /// </summary>
    public class ConflictObjectResult : ObjectResult
    {
        /// <summary>
        /// Creates a new <see cref="ConflictObjectResult"/> instance.
        /// </summary>
        /// <param name="error">Contains the errors to be returned to the client.</param>
        public ConflictObjectResult(object error)
            : base(error)
        {
            StatusCode = StatusCodes.Status409Conflict;
        }

        /// <summary>
        /// Creates a new <see cref="ConflictObjectResult"/> instance.
        /// </summary>
        /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
        public ConflictObjectResult(ModelStateDictionary modelState)
            : base(new SerializableError(modelState))
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            StatusCode = StatusCodes.Status409Conflict;
        }
    }
}
