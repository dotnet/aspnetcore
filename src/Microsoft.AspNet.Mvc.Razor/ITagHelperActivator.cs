// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Runtime.TagHelpers;

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
        /// <param name="tagHelper">The <see cref="ITagHelper"/> to activate.</param>
        /// <param name="context">The <see cref="ViewContext"/> for the executing view.</param>
        void Activate(ITagHelper tagHelper, ViewContext context);
    }
}