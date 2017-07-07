// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class MvcViewDocumentClassifierPass : DocumentClassifierPassBase
    {
        public static readonly string MvcViewDocumentKind = "mvc.1.0.view";

        protected override string DocumentKind => MvcViewDocumentKind;

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode) => true;

        protected override void OnDocumentStructureCreated(
            RazorCodeDocument codeDocument, 
            NamespaceDeclarationIntermediateNode @namespace, 
            ClassDeclarationIntermediateNode @class, 
            MethodDeclarationIntermediateNode method)
        {
            var filePath = codeDocument.GetRelativePath() ?? codeDocument.Source.FilePath;

            base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);

            @namespace.Content = "AspNetCore";

            @class.ClassName = CSharpIdentifier.GetClassNameFromPath(filePath);
            @class.BaseType = "global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>";
            @class.Modifiers.Clear();
            @class.Modifiers.Add("public");

            method.MethodName = "ExecuteAsync";
            method.Modifiers.Clear();
            method.Modifiers.Add("public");
            method.Modifiers.Add("async");
            method.Modifiers.Add("override");
            method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";
        }
    }
}
