// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Defines methods to create <see cref="RazorView"/> instances with a given <see cref="IRazorPage"/>.
    /// </summary>
    public interface IRazorViewFactory
    {
        /// <summary>
        /// Creates a <see cref="RazorView"/> providing it with the <see cref="IRazorPage"/> to execute.
        /// </summary>
        /// <param name="page">The <see cref="IRazorPage"/> instance to execute.</param>
        /// <param name="isPartial">Determines if the view is to be executed as a partial.</param>
        /// <returns>A <see cref="IView"/> instance that renders the contents of the <paramref name="page"/></returns>
        IView GetView([NotNull] IRazorPage page, bool isPartial);
    }
}