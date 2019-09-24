// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Identifies an action that supports the HTTP GET method.
    /// </summary>
    public class HttpGetAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new [] { "GET" };

        /// <summary>
        /// Creates a new <see cref="HttpGetAttribute"/>.
        /// </summary>
        public HttpGetAttribute()
            : base(_supportedMethods)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpGetAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpGetAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
        }
    }
}