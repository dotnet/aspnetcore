// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultDocumentClassifierPassFeature : RazorEngineFeatureBase
{
    public IList<Action<RazorCodeDocument, ClassDeclarationIntermediateNode>> ConfigureClass { get; } =
        new List<Action<RazorCodeDocument, ClassDeclarationIntermediateNode>>();

    public IList<Action<RazorCodeDocument, NamespaceDeclarationIntermediateNode>> ConfigureNamespace { get; } =
        new List<Action<RazorCodeDocument, NamespaceDeclarationIntermediateNode>>();

    public IList<Action<RazorCodeDocument, MethodDeclarationIntermediateNode>> ConfigureMethod { get; } =
        new List<Action<RazorCodeDocument, MethodDeclarationIntermediateNode>>();
}
