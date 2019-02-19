// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    /// <summary>
    /// Provides methods to activate properties of <see cref="ITagHelperComponent"/>s.
    /// </summary>
    public interface ITagHelperComponentPropertyActivator
    {
        /// <summary>
        /// Activates properties of the <paramref name="tagHelperComponent"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewContext"/> for the executing view.</param>
        /// <param name="tagHelperComponent">The <see cref="ITagHelperComponent"/> to activate properties of.</param>
        void Activate(ViewContext context, ITagHelperComponent tagHelperComponent);
    }
}
