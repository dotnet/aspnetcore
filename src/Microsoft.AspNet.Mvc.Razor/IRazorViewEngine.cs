// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// An <see cref="IViewEngine"/> used to render pages that use the Razor syntax.
    /// </summary>
    public interface IRazorViewEngine : IViewEngine
    {
        /// <summary>
        /// Finds a <see cref="IRazorPage"/> using the same view discovery semantics used in
        /// <see cref="IViewEngine.FindPartialView(ActionContext, string)"/>.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="viewName">The name or full path to the view.</param>
        /// <returns>A result representing the result of locating the <see cref="IRazorPage"/>.</returns>
        RazorPageResult FindPage(ActionContext context, string page);
    }
}