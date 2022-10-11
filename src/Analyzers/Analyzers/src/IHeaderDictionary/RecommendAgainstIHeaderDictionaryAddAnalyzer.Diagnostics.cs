// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.IHeaderDictionary;

public partial class RecommendAgainstIHeaderDictionaryAddAnalyzer : DiagnosticAnalyzer
{
    internal static class Diagnostics
    {
        internal static readonly DiagnosticDescriptor RecommendAgainstIHeaderDictionaryAdd = new(
            "ASP0015",
            "Recommend using IHeaderDictionary.Append or the indexer over IHeaderDictionary.Add",
            "Recommend using IHeaderDictionary.Append or the indexer over IHeaderDictionary.Add",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://aka.ms/aspnet/analyzers");

        public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics = ImmutableArray.Create(new[]
        {
            RecommendAgainstIHeaderDictionaryAdd
        });
    }
}
