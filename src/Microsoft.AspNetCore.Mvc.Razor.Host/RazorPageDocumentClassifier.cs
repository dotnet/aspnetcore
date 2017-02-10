// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
{
    public class RazorPageDocumentClassifier : DocumentClassifierPassBase
    {
        public static readonly string RazorPageDocumentKind = "mvc.1.0.razor-page";

        protected override string DocumentKind => RazorPageDocumentKind;

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            string routePrefix;
            return PageDirective.TryGetRouteTemplate(irDocument, out routePrefix);
        }

        protected override void OnDocumentStructureCreated(RazorCodeDocument codeDocument, NamespaceDeclarationIRNode @namespace, ClassDeclarationIRNode @class, RazorMethodDeclarationIRNode method)
        {
            base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);
            @class.BaseType = "Microsoft.AspNetCore.Mvc.RazorPages.Page";
            @class.Name = ClassName.GetClassNameFromPath(codeDocument.Source.Filename);
            @class.AccessModifier = "public";
            @namespace.Content = "AspNetCore";
            method.Name = "ExecuteAsync";
            method.Modifiers = new[] { "async", "override" };
            method.AccessModifier = "public";
            method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";
        }
    }
}
