// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Identifies an action that only supports the HTTP GET method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class HttpGetAttribute : Attribute, IActionHttpMethodProvider, IRouteTemplateProvider
    {
        private static readonly IEnumerable<string> _supportedMethods = new string[] { "GET" };

        /// <summary>
        /// Creates a new <see cref="HttpGetAttribute"/>.
        /// </summary>
        public HttpGetAttribute()
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpGetAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpGetAttribute([NotNull] string template)
        {
            Template = template;
        }

        /// <inheritdoc />
        public IEnumerable<string> HttpMethods
        {
            get { return _supportedMethods; }
        }

        /// <inheritdoc />
        public string Template { get; private set; }
    }
}