// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

public partial class DelegateEndpointFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticDescriptors.DetectMismatchedParameterOptionality.Id);

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case DelegateEndpointAnalyzer.DetectMismatchedParameterOptionalityRuleId:
                    context.RegisterCodeFix(
                        CodeAction.Create("Fix mismatched parameter optionality", cancellationToken => FixMismatchedParameterOptionality(context, cancellationToken), equivalenceKey: DiagnosticDescriptors.DetectMismatchedParameterOptionality.Id),
                        diagnostic);
                    break;
                default:
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
