// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal class RazorViewTemplateFactory : IViewTemplateFactory
    {
        private readonly RazorViewLookup _fileLookup;
        private readonly IRazorPageActivator _pageActivator;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly DiagnosticListener _diagnosticListener;

        public RazorViewTemplateFactory(
            RazorViewLookup fileLookup,
            IRazorPageActivator pageActivator,
            HtmlEncoder htmlEncoder,
            DiagnosticListener diagnosticListener)
        {
            _fileLookup = fileLookup;
            _pageActivator = pageActivator;
            _htmlEncoder = htmlEncoder;
            _diagnosticListener = diagnosticListener;
        }

        public async ValueTask<LocateViewResult> LocateViewAsync(ViewFactoryContext context)
        {
            var lookupResult = await _fileLookup.LocateViewAsync(context.ActionContext, context.Name, context.ExecutingFilePath, context.IsMainPage);
            if (lookupResult.Success)
            {
                var page = lookupResult.ViewEntry.PageFactory();

                IRazorPage[] viewStarts;
                if (context.IsMainPage)
                {
                    viewStarts = new IRazorPage[lookupResult.ViewStartEntries.Count];
                    for (var i = 0; i < viewStarts.Length; i++)
                    {
                        viewStarts[i] = lookupResult.ViewStartEntries[i].PageFactory();
                    }
                }
                else
                {
                    // For non-main pages, no ViewStarts to run.
                    viewStarts = Array.Empty<IRazorPage>();
                }

                var templatingSystem = new RazorViewTemplatingSystem(
                    _fileLookup,
                    _pageActivator,
                    _htmlEncoder,
                    _diagnosticListener,
                    page,
                    viewStarts);

                return new LocateViewResult(context.Name, templatingSystem);
            }

            return new LocateViewResult(context.Name, lookupResult.SearchedLocations);
        }
    }
}
