// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

internal static partial class PageDirectiveFeature
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

    public static bool TryGetPageDirective(ILogger logger, RazorProjectItem projectItem, [NotNullWhen(true)] out string? template)
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
                Log.MalformedPageDirective(logger, projectItem.FilePath, malformedNode.Diagnostics);
            }

            template = pageDirective.RouteTemplate;
            return true;
        }

        template = null;
        return false;
    }

    private sealed class PageDirectiveParserOptionsFeature : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature
    {
        public int Order { get; }

        public void Configure(RazorParserOptionsBuilder options)
        {
            options.ParseLeadingDirectives = true;
        }
    }

    private sealed class EmptyRazorProjectFileSystem : RazorProjectFileSystem
    {
        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            return Enumerable.Empty<RazorProjectItem>();
        }

        public override IEnumerable<RazorProjectItem> FindHierarchicalItems(string basePath, string path, string fileName)
        {
            return Enumerable.Empty<RazorProjectItem>();
        }


        public override RazorProjectItem GetItem(string path)
        {
            return GetItem(path, fileKind: null);
        }

        public override RazorProjectItem GetItem(string path, string? fileKind)
        {
            return new NotFoundProjectItem(string.Empty, path, fileKind);
        }

        private sealed class NotFoundProjectItem : RazorProjectItem
        {
            public NotFoundProjectItem(string basePath, string path, string? fileKind)
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

    private static partial class Log
    {
        [LoggerMessage(104, LogLevel.Warning, "The page directive at '{FilePath}' is malformed. Please fix the following issues: {Diagnostics}", EventName = "MalformedPageDirective", SkipEnabledCheck = true)]
        private static partial void MalformedPageDirective(ILogger logger, string filePath, string[] diagnostics);

        public static void MalformedPageDirective(ILogger logger, string filePath, IList<RazorDiagnostic> diagnostics)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                var messages = new string[diagnostics.Count];
                for (var i = 0; i < diagnostics.Count; i++)
                {
                    messages[i] = diagnostics[i].GetMessage(CultureInfo.CurrentCulture);
                }

                MalformedPageDirective(logger, filePath, messages);
            }
        }
    }
}
