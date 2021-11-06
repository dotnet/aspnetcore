// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Components.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Extensions.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ComponentInternalUsageDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] NamespaceParts = new[] { "RenderTree", "Components", "AspNetCore", "Microsoft", };

    private readonly InternalUsageAnalyzer _inner;

    public ComponentInternalUsageDiagnosticAnalyzer()
    {
        // We don't have in *internal* attribute in Blazor.
        _inner = new InternalUsageAnalyzer(IsInInternalNamespace, hasInternalAttribute: null, DiagnosticDescriptors.DoNotUseRenderTreeTypes);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DoNotUseRenderTreeTypes);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        _inner.Register(context);
    }

    private static bool IsInInternalNamespace(ISymbol symbol)
    {
        var @namespace = symbol?.ContainingNamespace;
        for (var i = 0; i < NamespaceParts.Length; i++)
        {
            if (@namespace == null || !string.Equals(NamespaceParts[i], @namespace.Name, StringComparison.Ordinal))
            {
                return false;
            }

            @namespace = @namespace.ContainingNamespace;
        }

        return @namespace.IsGlobalNamespace;
    }
}
