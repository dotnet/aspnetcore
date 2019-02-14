// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Represents properties and methods that are used by <see cref="RazorView"/> for execution.
    /// </summary>
    public interface IRazorPage
    {
        /// <summary>
        /// Gets or sets the view context of the rendering view.
        /// </summary>
        ViewContext ViewContext { get; set; }

        /// <summary>
        /// Gets or sets the body content.
        /// </summary>
        IHtmlContent BodyContent { get; set; }

        /// <summary>
        /// Gets or sets a flag that determines if the layout of this page is being rendered.
        /// </summary>
        /// <remarks>
        /// Sections defined in a page are deferred and executed as part of the layout page.
        /// When this flag is set, all write operations performed by the page are part of a
        /// section being rendered.
        /// </remarks>
        bool IsLayoutBeingRendered { get; set; }

        /// <summary>
        /// Gets the application base relative path to the page.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Gets or sets the path of a layout page.
        /// </summary>
        string Layout { get; set; }

        /// <summary>
        /// Gets or sets the sections that can be rendered by this page.
        /// </summary>
        IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }

        /// <summary>
        /// Gets the sections that are defined by this page.
        /// </summary>
        IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }

        /// <summary>
        /// Renders the page and writes the output to the <see cref="ViewContext.Writer"/>.
        /// </summary>
        /// <returns>A task representing the result of executing the page.</returns>
        Task ExecuteAsync();

        /// <summary>
        /// Verifies that all sections defined in <see cref="PreviousSectionWriters"/> were rendered, or
        /// the body was rendered if no sections were defined.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">if one or more sections were not rendered or if no sections were
        /// defined and the body was not rendered.</exception>
        void EnsureRenderedBodyOrSections();
    }
}