// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A formatter which does not have a supported content type and selects itself if no content type is passed to it.
    /// Sets the status code to 406 if selected.
    /// </summary>
    public class HttpNotAcceptableOutputFormatter : IOutputFormatter
    {
        /// <inheritdoc />
        public bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            return contentType == null;
        }

        /// <inheritdoc />
        public IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(Type declaredType,
                                                                            Type runtimeType,
                                                                            MediaTypeHeaderValue contentType)
        {
            return null;
        }

        /// <inheritdoc />
        public Task WriteAsync(OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;
            response.StatusCode = 406;
            return Task.FromResult(true);
        }
    }
}
