// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
