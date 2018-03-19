// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            @class.ClassName = CSharpIdentifier.GetClassNameFromPath(filePath);

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

            EnsureValidPageDirective(pageDirective);

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

        private void EnsureValidPageDirective(PageDirective pageDirective)
        {
            Debug.Assert(pageDirective != null);

            if (pageDirective.DirectiveNode.IsImported())
            {
                pageDirective.DirectiveNode.Diagnostics.Add(
                    RazorExtensionsDiagnosticFactory.CreatePageDirective_CannotBeImported(pageDirective.DirectiveNode.Source.Value));
            }
        }
    }
}
