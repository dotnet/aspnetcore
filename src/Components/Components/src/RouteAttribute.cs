// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Indicates that the associated component should match the specified route template pattern.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RouteAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of <see cref="RouteAttribute"/>.
        /// </summary>
        /// <param name="template">The route template.</param>
        public RouteAttribute(string template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            Template = template;
        }

        /// <summary>
        /// Gets the route template.
        /// </summary>
        public string Template { get; }
    }
}
