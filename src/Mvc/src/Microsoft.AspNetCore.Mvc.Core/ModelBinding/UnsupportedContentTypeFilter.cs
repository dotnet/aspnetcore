// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A filter that scans for <see cref="UnsupportedContentTypeException"/> in the
    /// <see cref="ActionContext.ModelState"/> and short-circuits the pipeline
    /// with an Unsupported Media Type (415) response.
    /// </summary>
    public class UnsupportedContentTypeFilter : IActionFilter
    {
        /// <inheritdoc />
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (HasUnsupportedContentTypeError(context))
            {
                context.Result = new UnsupportedMediaTypeResult();
            }
        }

        private bool HasUnsupportedContentTypeError(ActionExecutingContext context)
        {
            var modelState = context.ModelState;
            if (modelState.IsValid)
            {
                return false;
            }

            foreach (var kvp in modelState)
            {
                var errors = kvp.Value.Errors;
                for (int i = 0; i < errors.Count; i++)
                {
                    var error = errors[i];
                    if (error.Exception is UnsupportedContentTypeException)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
