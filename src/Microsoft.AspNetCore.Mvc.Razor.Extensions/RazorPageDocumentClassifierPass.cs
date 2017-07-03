// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class RazorPageDocumentClassifierPass : DocumentClassifierPassBase
    {
        public static readonly string RazorPageDocumentKind = "mvc.1.0.razor-page";

        protected override string DocumentKind => RazorPageDocumentKind;

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            return PageDirective.TryGetPageDirective(documentNode, out var directive);
        }

        protected override void OnDocumentStructureCreated(
            RazorCodeDocument codeDocument,
            NamespaceDeclarationIntermediateNode @namespace,
            ClassDeclarationIntermediateNode @class,
            MethodDeclarationIntermediateNode method)
        {
            var filePath = codeDocument.GetRelativePath() ?? codeDocument.Source.FilePath;

            base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);

            @namespace.Content = "AspNetCore";

            @class.BaseType = "global::Microsoft.AspNetCore.Mvc.RazorPages.Page";
            @class.Name = CSharpIdentifier.GetClassNameFromPath(filePath);

            @class.Modifiers.Clear();
            @class.Modifiers.Add("public");

            method.Name = "ExecuteAsync";
            method.Modifiers.Clear();
            method.Modifiers.Add("public");
            method.Modifiers.Add("async");
            method.Modifiers.Add("override");
            method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";

            EnsureValidPageDirective(codeDocument);
        }

        private void EnsureValidPageDirective(RazorCodeDocument codeDocument)
        {
            var document = codeDocument.GetDocumentIntermediateNode();
            var visitor = new Visitor();
            visitor.VisitDocument(document);

            if (visitor.DirectiveNode.IsImported())
            {
                visitor.DirectiveNode.Diagnostics.Add(
                    RazorExtensionsDiagnosticFactory.CreatePageDirective_CannotBeImported(visitor.DirectiveNode.Source.Value));
            }
        }

        private class Visitor : IntermediateNodeWalker
        {
            public DirectiveIntermediateNode DirectiveNode { get; private set; }

            public override void VisitDirective(DirectiveIntermediateNode node)
            {
                if (node.Descriptor == PageDirective.Directive)
                {
                    DirectiveNode = node;
                }
            }
        }
    }
}
