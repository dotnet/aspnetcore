// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public interface IRazorPage
    {
        ViewContext ViewContext { get; set; }

        IHtmlContent BodyContent { get; set; }

        bool IsLayoutBeingRendered { get; set; }

        string Path { get; set; }

        string Layout { get; set; }

        IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }

        IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }

        Task ExecuteAsync();

        void EnsureRenderedBodyOrSections();
    }
}
