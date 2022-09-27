// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class StartupAnalysisBuilder
{
    private readonly Dictionary<INamedTypeSymbol, List<object>> _analysesByType;
    private readonly StartupAnalyzer _analyzer;
    private readonly object _lock;

    public StartupAnalysisBuilder(StartupAnalyzer analyzer, StartupSymbols startupSymbols)
    {
        _analyzer = analyzer;
        StartupSymbols = startupSymbols;

#pragma warning disable RS1024 // Compare symbols correctly
        _analysesByType = new Dictionary<INamedTypeSymbol, List<object>>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
        _lock = new object();
    }

    public StartupSymbols StartupSymbols { get; }

    public StartupAnalysis Build()
    {
        lock (_lock)
        {
            return new StartupAnalysis(
                StartupSymbols,
                _analysesByType.ToImmutableDictionary(
                    k => k.Key,
                    v => v.Value.ToImmutableArray()));
        }
    }

    public void ReportAnalysis(ServicesAnalysis analysis)
    {
        ReportAnalysisCore(analysis.StartupType, analysis);
        _analyzer.OnServicesAnalysisCompleted(analysis);
    }

    public void ReportAnalysis(OptionsAnalysis analysis)
    {
        ReportAnalysisCore(analysis.StartupType, analysis);
        _analyzer.OnOptionsAnalysisCompleted(analysis);
    }

    public void ReportAnalysis(MiddlewareAnalysis analysis)
    {
        ReportAnalysisCore(analysis.StartupType, analysis);
        _analyzer.OnMiddlewareAnalysisCompleted(analysis);
    }

    private void ReportAnalysisCore(INamedTypeSymbol type, object analysis)
    {
        lock (_lock)
        {
            if (!_analysesByType.TryGetValue(type, out var list))
            {
                list = new List<object>();
                _analysesByType.Add(type, list);
            }

            list.Add(analysis);
        }
    }
}
