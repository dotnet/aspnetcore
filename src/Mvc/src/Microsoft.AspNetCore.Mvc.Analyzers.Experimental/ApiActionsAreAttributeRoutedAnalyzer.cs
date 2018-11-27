// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ApiActionsAreAttributeRoutedAnalyzer : ApiControllerAnalyzerBase
    {
        internal const string MethodNameKey = "MethodName";

        public ApiActionsAreAttributeRoutedAnalyzer()
            : base(DiagnosticDescriptors.MVC7000_ApiActionsMustBeAttributeRouted)
        {
        }

        protected override void InitializeWorker(ApiControllerAnalyzerContext analyzerContext)
        {
            analyzerContext.Context.RegisterSymbolAction(context =>
            {
                var method = (IMethodSymbol)context.Symbol;

                if (!analyzerContext.IsApiAction(method))
                {
                    return;
                }

                foreach (var attribute in method.GetAttributes())
                {
                    if (attribute.AttributeClass.IsAssignableFrom(analyzerContext.RouteAttribute))
                    {
                        return;
                    }
                }

                var properties = ImmutableDictionary.Create<string, string>(StringComparer.Ordinal)
                    .Add(MethodNameKey, method.Name);

                var location = method.Locations.Length > 0 ? method.Locations[0] : Location.None;
                context.ReportDiagnostic(Diagnostic.Create(
                    SupportedDiagnostic,
                    location,
                    properties: properties));

            }, SymbolKind.Method);
        }
    }
}
