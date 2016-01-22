// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Provides methods to activate properties on a <see cref="ITagHelper"/> instance.
    /// </summary>
    public interface ITagHelperActivator
    {
        /// <summary>
        /// When implemented in a type, activates an instantiated <see cref="ITagHelper"/>.
        /// </summary>
        /// <typeparam name="TTagHelper">The <see cref="ITagHelper"/> type.</typeparam>
        /// <param name="tagHelper">The <typeparamref name="TTagHelper"/> to activate.</param>
        /// <param name="context">The <see cref="ViewContext"/> for the executing view.</param>
        void Activate<TTagHelper>(TTagHelper tagHelper, ViewContext context) where TTagHelper : ITagHelper;
    }
}