// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

public partial class DelegateEndpointFixer : CodeFixProvider
{
    private static async Task<Document> FixMismatchedParameterOptionality(CodeFixContext context, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken);
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            var param = root.FindNode(diagnostic.Location.SourceSpan);
            if (param != null && param is ParameterSyntax parameterSyntax)
            {
                if (parameterSyntax.Type != null)
                {
                    var newParam = parameterSyntax.WithType(SyntaxFactory.NullableType(parameterSyntax.Type));
                    editor.ReplaceNode(parameterSyntax, newParam);   
                }
            }
        }
        return editor.GetChangedDocument();
    }
}
