// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
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
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var analyzerContext = new ViewFeaturesAnalyzerContext(compilationContext);

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
}
