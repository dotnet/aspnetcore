// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
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

        public bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            // ignore the contentType and just look at the content.
            // This formatter will be selected if the content is null. 
            // We check for Task as a user can directly create an ObjectContentResult with the unwrapped type.
            if(context.DeclaredType == typeof(void) || context.DeclaredType == typeof(Task))
            {
                return true;
            }

            return TreatNullValueAsNoContent && context.Object == null;
        }

        public IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(
            Type declaredType, 
            Type runtimeType, 
            MediaTypeHeaderValue contentType)
        {
            return null;
        }

        public Task WriteAsync(OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;
            response.ContentLength = 0;

            // Only set the status code if its not already set.
            // TODO: By default the status code is set to 200. 
            // https://github.com/aspnet/HttpAbstractions/issues/114
            response.StatusCode = 204;
            return Task.FromResult(true);
        }
    }
}
