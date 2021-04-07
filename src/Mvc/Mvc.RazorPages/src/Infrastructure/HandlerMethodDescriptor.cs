// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Represents a description of a handler method.
    /// </summary>
    public class HandlerMethodDescriptor
    {
        /// <summary>
        /// Gets or sets the <see cref="MethodInfo"/>.
        /// </summary>
        public MethodInfo MethodInfo { get; set; }

        /// <summary>
        /// Gets or sets the http method.
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the method.
        /// </summary>
        public IList<HandlerParameterDescriptor> Parameters { get; set; }
    }
}
