// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    // Implements IRazorPage so that RazorPageBase-derived classes don't get activated twice.
    //
    // The page gets activated before handler methods run, but the RazorView will also activate
    // each page.
    public class RazorPageAdapter : IRazorPage, IModelTypeProvider
    {
        private readonly RazorPageBase _page;
        private readonly Type _modelType;

        public RazorPageAdapter(RazorPageBase page, Type modelType)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _modelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
        }

        public ViewContext ViewContext
        {
            get { return _page.ViewContext; }
            set { _page.ViewContext = value; }
        }

        public IHtmlContent BodyContent
        {
            get { return _page.BodyContent; }
            set { _page.BodyContent = value; }
        }

        public bool IsLayoutBeingRendered
        {
            get { return _page.IsLayoutBeingRendered; }
            set { _page.IsLayoutBeingRendered = value; }
        }

        public string Path
        {
            get { return _page.Path; }
            set { _page.Path = value; }
        }

        public string Layout
        {
            get { return _page.Layout; }
            set { _page.Layout = value; }
        }

        public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters
        {
            get { return _page.PreviousSectionWriters; }
            set { _page.PreviousSectionWriters = value; }
        }

        public IDictionary<string, RenderAsyncDelegate> SectionWriters => _page.SectionWriters;

        public void EnsureRenderedBodyOrSections()
        {
            _page.EnsureRenderedBodyOrSections();
        }

        public Task ExecuteAsync()
        {
            return _page.ExecuteAsync();
        }

        Type IModelTypeProvider.GetModelType() => _modelType;
    }
}
