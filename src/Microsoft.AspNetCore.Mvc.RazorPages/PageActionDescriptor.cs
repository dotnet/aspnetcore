// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    [DebuggerDisplay("{" + nameof(ViewEnginePath) + "}")]
    public class PageActionDescriptor : ActionDescriptor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageActionDescriptor"/>.
        /// </summary>
        public PageActionDescriptor()
        {
        }

        /// <summary>
        /// A copy constructor for <see cref="PageActionDescriptor"/>.
        /// </summary>
        /// <param name="other">The <see cref="PageActionDescriptor"/> to copy from.</param>
        public PageActionDescriptor(PageActionDescriptor other)
        {
            RelativePath = other.RelativePath;
            ViewEnginePath = other.ViewEnginePath;
        }

        /// <summary>
        /// Gets or sets the application root relative path for the page.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets the path relative to the base path for page discovery.
        /// </summary>
        public string ViewEnginePath { get; set; }
    }
}