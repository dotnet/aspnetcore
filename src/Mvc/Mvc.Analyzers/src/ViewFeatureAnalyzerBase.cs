// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

public abstract class ViewFeatureAnalyzerBase : DiagnosticAnalyzer
{
    public ViewFeatureAnalyzerBase(DiagnosticDescriptor diagnosticDescriptor)
    {
        SupportedDiagnostic = diagnosticDescriptor;
        SupportedDiagnostics = ImmutableArray.Create(new[] { SupportedDiagnostic });
    }

    protected DiagnosticDescriptor SupportedDiagnostic { get; }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public sealed override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new ViewFeaturesAnalyzerContext(context);

            // Only do work if we can locate IHtmlHelper.
            if (analyzerContext.HtmlHelperType == null)
            {
                return;
            }

            InitializeWorker(analyzerContext);
        });
    }

    protected abstract void InitializeWorker(ViewFeaturesAnalyzerContext analyzerContext);
}
