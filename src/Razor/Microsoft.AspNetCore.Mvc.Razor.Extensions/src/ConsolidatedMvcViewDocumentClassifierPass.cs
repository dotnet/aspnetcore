// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ConsolidatedMvcViewDocumentClassifierPass : DocumentClassifierPassBase
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
            base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);

            if (!codeDocument.TryComputeNamespace(fallbackToRootNamespace: false, out var namespaceName))
            {
                @namespace.Content = "AspNetCoreGeneratedDocument";
            }
            else
            {
                @namespace.Content = namespaceName;
            }

            if (!TryComputeClassName(codeDocument, out var className))
            {
                // It's possible for a Razor document to not have a file path.
                // Eg. When we try to generate code for an in memory document like default imports.
                var checksum = Checksum.BytesToString(codeDocument.Source.GetChecksum());
                @class.ClassName = $"AspNetCore_{checksum}";
            }
            else
            {
                @class.ClassName = className;
            }
            

            @class.BaseType = "global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>";
            @class.Modifiers.Clear();
            @class.Modifiers.Add("internal");
            @class.Modifiers.Add("sealed");

            method.MethodName = "ExecuteAsync";
            method.Modifiers.Clear();
            method.Modifiers.Add("public");
            method.Modifiers.Add("async");
            method.Modifiers.Add("override");
            method.ReturnType = $"global::{typeof(System.Threading.Tasks.Task).FullName}";
        }

        private bool TryComputeClassName(RazorCodeDocument codeDocument, out string className)
        {
            var filePath = codeDocument.Source.RelativePath ?? codeDocument.Source.FilePath;
            if (string.IsNullOrEmpty(filePath))
            {
                className = null;
                return false;
            }

            className = GetClassNameFromPath(filePath);
            return true;
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