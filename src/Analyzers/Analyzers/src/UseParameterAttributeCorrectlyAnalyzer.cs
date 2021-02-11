using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class UseParameterAttributeCorrectlyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
#pragma warning disable RS2008 // Enable analyzer release tracking
            id: "ASP0002",
#pragma warning restore RS2008 // Enable analyzer release tracking
            title: "A property that has 'ParameterAttribute' should be an auto-property",
            messageFormat: "The property '{0}' should be auto-property",
            category: "",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(context =>
            {
                if (context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.MicrosoftAspNetCoreComponentsParameterAttribute) is INamedTypeSymbol parameterAttributeSymbol)
                {
                    context.RegisterSymbolAction(context =>
                    {
                        var symbol = (IPropertySymbol)context.Symbol;
                        if (symbol.HasAttribute(parameterAttributeSymbol) && !symbol.IsAutoProperty())
                        {
                            context.ReportDiagnostic(Diagnostic.Create(s_rule, symbol.Locations.FirstOrDefault(), symbol.Name));
                        }
                    }, SymbolKind.Property);
                }
            });
        }
    }
}
