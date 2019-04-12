// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    internal static class PageDirectiveFeature
    {
        private static readonly RazorProjectEngine PageDirectiveEngine = RazorProjectEngine.Create(RazorConfiguration.Default, new EmptyRazorProjectFileSystem(), builder =>
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

            var codeDocument = PageDirectiveEngine.Process(projectItem);

            var documentIRNode = codeDocument.GetDocumentIntermediateNode();
            if (PageDirective.TryGetPageDirective(documentIRNode, out var pageDirective))
            {
                if (pageDirective.DirectiveNode is MalformedDirectiveIntermediateNode malformedNode)
                {
                    logger.MalformedPageDirective(projectItem.FilePath, malformedNode.Diagnostics);
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

        private class EmptyRazorProjectFileSystem : RazorProjectFileSystem
        {
            public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
            {
                return Enumerable.Empty<RazorProjectItem>();
            }

            public override IEnumerable<RazorProjectItem> FindHierarchicalItems(string basePath, string path, string fileName)
            {
                return Enumerable.Empty<RazorProjectItem>();
            }

            [Obsolete("Use GetItem(string path, string fileKind) instead.")]
            public override RazorProjectItem GetItem(string path)
            {
                return GetItem(path, fileKind: null);
            }

            public override RazorProjectItem GetItem(string path, string fileKind)
            {
                return new NotFoundProjectItem(string.Empty, path, fileKind);
            }

            private class NotFoundProjectItem : RazorProjectItem
            {
                public NotFoundProjectItem(string basePath, string path, string fileKind)
                {
                    BasePath = basePath;
                    FilePath = path;
                    FileKind = fileKind ?? FileKinds.GetFileKindFromFilePath(FilePath);
                }

                /// <inheritdoc />
                public override string BasePath { get; }

                /// <inheritdoc />
                public override string FilePath { get; }

                /// <inheritdoc />
                public override string FileKind { get; }

                /// <inheritdoc />
                public override bool Exists => false;

                /// <inheritdoc />
                public override string PhysicalPath => throw new NotSupportedException();

                /// <inheritdoc />
                public override Stream Read() => throw new NotSupportedException();
            }
        }
    }
}
