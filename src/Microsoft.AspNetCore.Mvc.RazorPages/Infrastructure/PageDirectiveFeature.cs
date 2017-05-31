// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public static class PageDirectiveFeature
    {
        private static readonly RazorEngine PageDirectiveEngine = RazorEngine.Create(builder =>
        {
            for (var i = builder.Phases.Count - 1; i >= 0; i--)
            {
                var phase = builder.Phases[i];
                builder.Phases.RemoveAt(i);
                if (phase is IRazorDocumentClassifierPhase)
                {
                    break;
                }
            }

            RazorExtensions.Register(builder);
            builder.Features.Add(new PageDirectiveParserOptionsFeature());
        });

        public static bool TryGetPageDirective(RazorProjectItem projectItem, out string template)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);
            return TryGetPageDirective(sourceDocument, out template);
        }

        public static bool TryGetPageDirective(Func<Stream> streamFactory, out string template)
        {
            if (streamFactory == null)
            {
                throw new ArgumentNullException(nameof(streamFactory));
            }

            using (var stream = streamFactory())
            {
                var sourceDocument = RazorSourceDocument.ReadFrom(stream, fileName: "Parse.cshtml");
                return TryGetPageDirective(sourceDocument, out template);
            }
        }

        private static bool TryGetPageDirective(RazorSourceDocument sourceDocument, out string template)
        {
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            PageDirectiveEngine.Process(codeDocument);

            if (PageDirective.TryGetPageDirective(codeDocument.GetIRDocument(), out var pageDirective))
            {
                template = pageDirective.RouteTemplate;
                return true;
            }

            template = null;
            return false;
        }

        private class PageDirectiveParserOptionsFeature : RazorEngineFeatureBase, IRazorParserOptionsFeature
        {
            public int Order { get; }

            public void Configure(RazorParserOptionsBuilder options)
            {
                options.ParseOnlyLeadingDirectives = true;
            }
        }
    }
}
