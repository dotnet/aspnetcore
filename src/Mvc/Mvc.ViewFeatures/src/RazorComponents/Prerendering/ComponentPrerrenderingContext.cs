// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server
{
    /// <summary>
    /// The context for prerendering a component.
    /// </summary>
    public class ComponentPrerenderingContext
    {
        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        public Type ComponentType { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the component.
        /// </summary>
        public ParameterCollection Parameters { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HttpContext"/> in which the prerendering has been initiated.
        /// </summary>
        public HttpContext Context { get; set; }
    }
}
