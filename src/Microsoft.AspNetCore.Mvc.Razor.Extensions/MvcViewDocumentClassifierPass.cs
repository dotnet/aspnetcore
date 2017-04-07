// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor.Extensions.Internal;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class MvcViewDocumentClassifierPass : DocumentClassifierPassBase
    {
        public readonly string MvcViewDocumentKind = "mvc.1.0.view";

        protected override string DocumentKind => MvcViewDocumentKind;

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIRNode irDocument) => true;

        protected override void OnDocumentStructureCreated(
            RazorCodeDocument codeDocument, 
            NamespaceDeclarationIRNode @namespace, 
            ClassDeclarationIRNode @class, 
            RazorMethodDeclarationIRNode method)
        {
            var filePath = codeDocument.GetRelativePath() ?? codeDocument.Source.FileName;

            base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);
            @class.Name = ClassName.GetClassNameFromPath(filePath);
            @class.BaseType = "global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>";
            @class.AccessModifier = "public";
            @namespace.Content = "AspNetCore";
            method.Name = "ExecuteAsync";
            method.Modifiers = new[] { "async", "override" };
            method.AccessModifier = "public";
            method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";
        }
    }
}
