// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that returns a <see cref="StatusCodes.Status400BadRequest"/> response and performs
    /// content negotiation on an <see cref="HttpError"/> based on a <see cref="ModelStateDictionary"/>.
    /// </summary>
    public class InvalidModelStateResult : ObjectResult
    {
        /// <summary>Initializes a new instance of the <see cref="InvalidModelStateResult"/> class.</summary>
        /// <param name="modelState">The model state to include in the error.</param>
        /// <param name="includeErrorDetail">
        /// <see langword="true"/> if the error should include exception messages; otherwise, <see langword="false"/>.
        /// </param>
        public InvalidModelStateResult(ModelStateDictionary modelState, bool includeErrorDetail)
            : base(new HttpError(modelState, includeErrorDetail))
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            ModelState = modelState;
            IncludeErrorDetail = includeErrorDetail;
        }

        /// <summary>
        /// Gets the model state to include in the error.
        /// </summary>
        public ModelStateDictionary ModelState { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the error should include exception messages.
        /// </summary>
        public bool IncludeErrorDetail { get; private set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return base.ExecuteResultAsync(context);
        }
    }
}