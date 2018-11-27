// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public abstract class RazorPage : IRazorPage
    {
        public ViewContext ViewContext { get; set; }

        public IHtmlContent BodyContent { get; set; }

        public bool IsLayoutBeingRendered { get; set; }

        public string Path { get; set; }

        public string Layout { get; set; }

        public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }

        public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }

        public ITempDataDictionary TempData { get; }

        public void EnsureRenderedBodyOrSections()
        {
        }

        public void DefineSection(string name, RenderAsyncDelegate section)
        {
        }

        public abstract Task ExecuteAsync();

        public void BeginContext(int position, int length, bool isLiteral)
        {
        }

        public void EndContext()
        {
        }

        public TTagHelper CreateTagHelper<TTagHelper>() where TTagHelper : ITagHelper
        {
            throw new NotImplementedException();
        }
    }
}
