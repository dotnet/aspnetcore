// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultDocumentClassifierPass : DocumentClassifierPassBase
{
    public override int Order => DefaultFeatureOrder;

    protected override string DocumentKind => "default";

    protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        return true;
    }

    protected override void OnDocumentStructureCreated(
        RazorCodeDocument codeDocument,
        NamespaceDeclarationIntermediateNode @namespace,
        ClassDeclarationIntermediateNode @class,
        MethodDeclarationIntermediateNode method)
    {
        var configuration = Engine.GetFeature<DefaultDocumentClassifierPassFeature>();
        if (configuration != null)
        {
            for (var i = 0; i < configuration.ConfigureClass.Count; i++)
            {
                var configureClass = configuration.ConfigureClass[i];
                configureClass(codeDocument, @class);
            }

            for (var i = 0; i < configuration.ConfigureNamespace.Count; i++)
            {
                var configureNamespace = configuration.ConfigureNamespace[i];
                configureNamespace(codeDocument, @namespace);
            }

            for (var i = 0; i < configuration.ConfigureMethod.Count; i++)
            {
                var configureMethod = configuration.ConfigureMethod[i];
                configureMethod(codeDocument, @method);
            }
        }
    }
}
