// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.Extensions.Logging;

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

        public static bool TryGetPageDirective(ILogger logger, RazorProjectItem projectItem, out string template)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);
            return TryGetPageDirective(logger, sourceDocument, out template);
        }

        static bool TryGetPageDirective(
            ILogger logger,
            RazorSourceDocument sourceDocument,
            out string template)
        {
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            PageDirectiveEngine.Process(codeDocument);

            var documentIRNode = codeDocument.GetDocumentIntermediateNode();
            if (PageDirective.TryGetPageDirective(documentIRNode, out var pageDirective))
            {
                if (pageDirective.DirectiveNode is MalformedDirectiveIntermediateNode malformedNode)
                {
                    logger.MalformedPageDirective(sourceDocument.FilePath, malformedNode.Diagnostics);
                }

                template = pageDirective.RouteTemplate;
                return true;
            }

            template = null;
            return false;
        }

        private class PageDirectiveParserOptionsFeature : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature
        {
            public int Order { get; }

            public void Configure(RazorParserOptionsBuilder options)
            {
                options.ParseLeadingDirectives = true;
            }
        }
    }
}
