// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A context object for <see cref="IPageApplicationModelProvider"/>.
    /// </summary>
    public class PageApplicationModelProviderContext
    {
        public PageApplicationModelProviderContext(PageActionDescriptor descriptor, TypeInfo pageTypeInfo)
        {
            ActionDescriptor = descriptor;
            PageType = pageTypeInfo;
        }

        /// <summary>
        /// Gets the <see cref="PageActionDescriptor"/>.
        /// </summary>
        public PageActionDescriptor ActionDescriptor { get; }

        /// <summary>
        /// Gets the page <see cref="TypeInfo"/>.
        /// </summary>
        public TypeInfo PageType { get; }

        /// <summary>
        /// Gets or sets the <see cref="ApplicationModels.PageApplicationModel"/>.
        /// </summary>
        public PageApplicationModel PageApplicationModel { get; set; }
    }
}