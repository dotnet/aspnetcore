// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc;

namespace System.Web.Http
{
    /// <summary>
    /// An action result that returns a <see cref="System.Net.HttpStatusCode.BadRequest"/> response and performs
    /// content negotiation on an <see cref="HttpError"/> based on a <see cref="ModelStateDictionary"/>.
    /// </summary>
    public class InvalidModelStateResult : ObjectResult
    {
        /// <summary>Initializes a new instance of the <see cref="InvalidModelStateResult"/> class.</summary>
        /// <param name="modelState">The model state to include in the error.</param>
        /// <param name="includeErrorDetail">
        /// <see langword="true"/> if the error should include exception messages; otherwise, <see langword="false"/>.
        /// </param>
        public InvalidModelStateResult([NotNull] ModelStateDictionary modelState, bool includeErrorDetail)
            : base(new HttpError(modelState, includeErrorDetail))
        {
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
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await base.ExecuteResultAsync(context);
        }
    }
}