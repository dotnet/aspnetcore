// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class RazorPageDocumentClassifierPass : DocumentClassifierPassBase
    {
        public static readonly string RazorPageDocumentKind = "mvc.1.0.razor-page";
        public static readonly string RouteTemplateKey = "RouteTemplate";

        private static readonly RazorProjectEngine LeadingDirectiveParsingEngine = RazorProjectEngine.Create(
            RazorConfiguration.Default,
            RazorProjectFileSystem.Create("/"),
            builder =>
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
                builder.Features.Add(new LeadingDirectiveParserOptionsFeature());
            });

        protected override string DocumentKind => RazorPageDocumentKind;

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            return PageDirective.TryGetPageDirective(documentNode, out var pageDirective);
        }

        protected override void OnDocumentStructureCreated(
            RazorCodeDocument codeDocument,
            NamespaceDeclarationIntermediateNode @namespace,
            ClassDeclarationIntermediateNode @class,
            MethodDeclarationIntermediateNode method)
        {
            base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);

            @namespace.Content = "AspNetCore";

            @class.BaseType = "global::Microsoft.AspNetCore.Mvc.RazorPages.Page";

            var filePath = codeDocument.Source.RelativePath ?? codeDocument.Source.FilePath;
            if (string.IsNullOrEmpty(filePath))
            {
                // It's possible for a Razor document to not have a file path.
                // Eg. When we try to generate code for an in memory document like default imports.
                var checksum = Checksum.BytesToString(codeDocument.Source.GetChecksum());
                @class.ClassName = $"AspNetCore_{checksum}";
            }
            else
            {
                @class.ClassName = GetClassNameFromPath(filePath);
            }

            @class.Modifiers.Clear();
            @class.Modifiers.Add("public");

            method.MethodName = "ExecuteAsync";
            method.Modifiers.Clear();
            method.Modifiers.Add("public");
            method.Modifiers.Add("async");
            method.Modifiers.Add("override");
            method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";

            var document = codeDocument.GetDocumentIntermediateNode();
            PageDirective.TryGetPageDirective(document, out var pageDirective);

            EnsureValidPageDirective(codeDocument, pageDirective);

            AddRouteTemplateMetadataAttribute(@namespace, @class, pageDirective);
        }

        private static void AddRouteTemplateMetadataAttribute(NamespaceDeclarationIntermediateNode @namespace, ClassDeclarationIntermediateNode @class, PageDirective pageDirective)
        {
            if (string.IsNullOrEmpty(pageDirective.RouteTemplate))
            {
                return;
            }

            var classIndex = @namespace.Children.IndexOf(@class);
            if (classIndex == -1)
            {
                return;
            }

            var metadataAttributeNode = new RazorCompiledItemMetadataAttributeIntermediateNode
            {
                Key = RouteTemplateKey,
                Value = pageDirective.RouteTemplate,
            };
            // Metadata attributes need to be inserted right before the class declaration.
            @namespace.Children.Insert(classIndex, metadataAttributeNode);
        }

        private void EnsureValidPageDirective(RazorCodeDocument codeDocument, PageDirective pageDirective)
        {
            Debug.Assert(pageDirective != null);

            if (pageDirective.DirectiveNode.IsImported())
            {
                pageDirective.DirectiveNode.Diagnostics.Add(
                    RazorExtensionsDiagnosticFactory.CreatePageDirective_CannotBeImported(pageDirective.DirectiveNode.Source.Value));
            }
            else
            {
                // The document contains a page directive and it is not imported.
                // We now want to make sure this page directive exists at the top of the file.
                // We are going to do that by re-parsing the document until the very first line that is not Razor comment
                // or whitespace. We then make sure the page directive still exists in the re-parsed IR tree.
                var leadingDirectiveCodeDocument = RazorCodeDocument.Create(codeDocument.Source);
                LeadingDirectiveParsingEngine.Engine.Process(leadingDirectiveCodeDocument);

                var leadingDirectiveDocumentNode = leadingDirectiveCodeDocument.GetDocumentIntermediateNode();
                if (!PageDirective.TryGetPageDirective(leadingDirectiveDocumentNode, out var _))
                {
                    // The page directive is not the leading directive. Add an error.
                    pageDirective.DirectiveNode.Diagnostics.Add(
                        RazorExtensionsDiagnosticFactory.CreatePageDirective_MustExistAtTheTopOfFile(pageDirective.DirectiveNode.Source.Value));
                }
            }
        }

        private class LeadingDirectiveParserOptionsFeature : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature
        {
            public int Order { get; }

            public void Configure(RazorParserOptionsBuilder options)
            {
                options.ParseLeadingDirectives = true;
            }
        }

        private static string GetClassNameFromPath(string path)
        {
            const string cshtmlExtension = ".cshtml";

            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (path.EndsWith(cshtmlExtension, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - cshtmlExtension.Length);
            }

            return CSharpIdentifier.SanitizeIdentifier(path);
        }
    }
}
