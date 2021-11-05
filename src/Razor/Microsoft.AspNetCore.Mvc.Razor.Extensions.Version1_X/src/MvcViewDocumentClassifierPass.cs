// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

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
        base.OnDocumentStructureCreated(codeDocument, @namespace, @class, method);

        @namespace.Content = "AspNetCore";

        var filePath = codeDocument.Source.RelativePath ?? codeDocument.Source.FilePath;
        if (string.IsNullOrEmpty(filePath))
        {
            // It's possible for a Razor document to not have a file path.
            // Eg. When we try to generate code for an in memory document like default imports.
            var checksum = BytesToString(codeDocument.Source.GetChecksum());
            @class.ClassName = $"AspNetCore_{checksum}";
        }
        else
        {
            @class.ClassName = CSharpIdentifier.GetClassNameFromPath(filePath);
        }

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

    private static string BytesToString(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        var result = new StringBuilder(bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
        {
            // The x2 format means lowercase hex, where each byte is a 2-character string.
            result.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return result.ToString();
    }
}
