// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.PageExecutionInstrumentation;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents properties and methods that are used by <see cref="RazorView"/> for execution.
    /// </summary>
    public interface IRazorPage
    {
        /// <summary>
        /// Gets or sets the view context of the renderign view.
        /// </summary>
        ViewContext ViewContext { get; set; }

        /// <summary>
        /// Gets or sets the action invoked to render the body.
        /// </summary>
        Action<TextWriter> RenderBodyDelegate { get; set; }

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
        /// Gets or sets a value that determines if the current instance of <see cref="IRazorPage"/> is being executed
        /// from a partial view.
        /// </summary>
        bool IsPartial { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="IPageExecutionContext"/> instance used to instrument the page execution.
        /// </summary>
        IPageExecutionContext PageExecutionContext { get; set; }

        /// <summary>
        /// Gets or sets the sections that can be rendered by this page.
        /// </summary>
        Dictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }

        /// <summary>
        /// Gets the sections that are defined by this page.
        /// </summary>
        Dictionary<string, RenderAsyncDelegate> SectionWriters { get; }

        /// <summary>
        /// Renders the page and writes the output to the <see cref="ViewContext.Writer"/>.
        /// </summary>
        /// <returns>A task representing the result of executing the page.</returns>
        Task ExecuteAsync();

        /// <summary>
        /// Verifies that RenderBody is called and that RenderSection is called for all sections for a page that is
        /// part of view execution hierarchy.
        /// </summary>
        void EnsureBodyAndSectionsWereRendered();
    }
}