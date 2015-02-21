// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Identifies an action that only supports the HTTP DELETE method.
    /// </summary>
    public class HttpDeleteAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new string[] { "DELETE" };

        /// <summary>
        /// Creates a new <see cref="HttpDeleteAttribute"/>.
        /// </summary>
        public HttpDeleteAttribute()
            : base(_supportedMethods)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpDeleteAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpDeleteAttribute([NotNull] string template)
            : base(_supportedMethods, template)
        {
        }
    }
}