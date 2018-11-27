// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Provides methods to create and initialize tag helpers.
    /// </summary>
    public interface ITagHelperFactory
    {
        /// <summary>
        /// Creates a new tag helper for the specified <paramref name="context"/>.
        /// </summary>
        /// <param name="context"><see cref="ViewContext"/> for the executing view.</param>
        /// <returns>The tag helper.</returns>
        TTagHelper CreateTagHelper<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper;
    }
}
