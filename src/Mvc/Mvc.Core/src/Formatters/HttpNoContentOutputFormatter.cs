// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Sets the status code to 204 if the content is null.
    /// </summary>
    public class HttpNoContentOutputFormatter : IOutputFormatter
    {
        /// <summary>
        /// Indicates whether to select this formatter if the returned value from the action
        /// is null.
        /// </summary>
        public bool TreatNullValueAsNoContent { get; set; } = true;

        /// <inheritdoc />
        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            // ignore the contentType and just look at the content.
            // This formatter will be selected if the content is null.
            // We check for Task as a user can directly create an ObjectContentResult with the unwrapped type.
            if (context.ObjectType == typeof(void) || context.ObjectType == typeof(Task))
            {
                return true;
            }

            return TreatNullValueAsNoContent && context.Object == null;
        }

        /// <inheritdoc />
        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentLength = 0;

            if (response.StatusCode == StatusCodes.Status200OK)
            {
                response.StatusCode = StatusCodes.Status204NoContent;
            }

            return Task.CompletedTask;
        }
    }
}
