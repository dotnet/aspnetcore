// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Initializes an <see cref="ITagHelper"/> before it's executed.
    /// </summary>
    /// <typeparam name="TTagHelper">The <see cref="ITagHelper"/> type.</typeparam>
    public interface ITagHelperInitializer<TTagHelper>
        where TTagHelper : ITagHelper
    {
        /// <summary>
        /// Initializes the <typeparamref name="TTagHelper"/>.
        /// </summary>
        /// <param name="helper">The <typeparamref name="TTagHelper"/> to initialize.</param>
        /// <param name="context">The <see cref="ViewContext"/> for the executing view.</param>
        void Initialize(TTagHelper helper, ViewContext context);
    }
}