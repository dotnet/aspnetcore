// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets the status code to 204 if the content is null.
    /// </summary>
    public class NoContentFormatter : IOutputFormatter
    {
        public bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            // ignore the contentType and just look at the content.
            // This formatter will be selected if the content is null. 
            return context.Object == null;
        }

        public Task WriteAsync(OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;
            response.ContentLength = 0;

            // Only set the status code if its not already set.
            if (response.StatusCode == 0)
            {
                response.StatusCode = 204;
            }

            return Task.FromResult<bool>(true);
        }
    }
}
