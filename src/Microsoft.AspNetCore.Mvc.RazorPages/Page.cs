// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// A base class for a Razor page.
    /// </summary>
    public abstract class Page : IRazorPage
    {
        /// <inheritdoc />
        public IHtmlContent BodyContent { get; set; }

        /// <inheritdoc />
        public bool IsLayoutBeingRendered { get; set; }

        /// <inheritdoc />
        public string Layout { get; set; }

        /// <inheritdoc />
        public string Path { get; set; }

        /// <inheritdoc />
        public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }

        /// <inheritdoc />
        public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }

        /// <summary>
        /// The <see cref="PageContext"/>.
        /// </summary>
        public PageContext PageContext { get; set; }

        /// <inheritdoc />
        public ViewContext ViewContext { get; set; }

        /// <inheritdoc />
        public void EnsureRenderedBodyOrSections()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public abstract Task ExecuteAsync();
    }
}
