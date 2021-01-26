// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class StartupAnalysisBuilder
    {
        private readonly Dictionary<INamedTypeSymbol, List<object>> _analysesByType;
        private readonly StartupAnalyzer _analyzer;
        private readonly object _lock;

        public StartupAnalysisBuilder(StartupAnalyzer analyzer, StartupSymbols startupSymbols)
        {
            _analyzer = analyzer;
            StartupSymbols = startupSymbols;

            _analysesByType = new Dictionary<INamedTypeSymbol, List<object>>();
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
}
