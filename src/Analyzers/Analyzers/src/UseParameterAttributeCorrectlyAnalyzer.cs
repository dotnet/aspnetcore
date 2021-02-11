using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                    context.RegisterSymbolAction(async context =>
                    {
                        var symbol = (IPropertySymbol)context.Symbol;
                        if (symbol.HasAttribute(parameterAttributeSymbol) && !symbol.IsAutoProperty() &&
                            !await IsSameSemanticAsAutoPropertyAsync(symbol, context.CancellationToken).ConfigureAwait(false))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(s_rule, symbol.Locations.FirstOrDefault(), symbol.Name));
                        }
                    }, SymbolKind.Property);
                }
            });
        }

        private async Task<bool> IsSameSemanticAsAutoPropertyAsync(IPropertySymbol symbol, CancellationToken cancellationToken)
        {
            // This is not the preferred way to do things. There is a current work to support C# and VB with separate projects.
            // When that's done, this should be made abstract and have different C# and VB implementations.
            if (symbol.Language == LanguageNames.CSharp && symbol.DeclaringSyntaxReferences.Length == 1 &&
                await symbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is PropertyDeclarationSyntax syntax &&
                syntax.AccessorList.Accessors.Count == 2)
            {
                var getterAccessor = syntax.AccessorList.Accessors[0];
                var setterAccessor = syntax.AccessorList.Accessors[1];
                if (getterAccessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    // Swap if necessary.
                    (getterAccessor, setterAccessor) = (setterAccessor, getterAccessor);
                }

                if (!getterAccessor.IsKind(SyntaxKind.GetAccessorDeclaration) || !setterAccessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    return false;
                }

                IdentifierNameSyntax? identifierUsedInGetter = GetIdentifierUsedInGetter(getterAccessor);
                if (identifierUsedInGetter is null)
                {
                    return false;
                }

                IdentifierNameSyntax? identifierUsedInSetter = GetIdentifierUsedInSetter(setterAccessor);
                return identifierUsedInGetter.Identifier.ValueText == identifierUsedInSetter?.Identifier.ValueText;


            }

            return false;
        }

        private static IdentifierNameSyntax? GetIdentifierUsedInGetter(AccessorDeclarationSyntax getter)
        {
            if (getter.Body is { Statements: { Count: 1 } } && getter.Body.Statements[0] is ReturnStatementSyntax returnStatement)
            {
                return returnStatement.Expression as IdentifierNameSyntax;
            }

            return getter.ExpressionBody?.Expression as IdentifierNameSyntax;
        }

        private IdentifierNameSyntax? GetIdentifierUsedInSetter(AccessorDeclarationSyntax setter)
        {
            AssignmentExpressionSyntax? assignmentExpression = null;
            if (setter.Body is not null)
            {
                if (setter.Body.Statements.Count == 1)
                {
                    assignmentExpression = (setter.Body.Statements[0] as ExpressionStatementSyntax)?.Expression as AssignmentExpressionSyntax;
                }
            }
            else
            {
                assignmentExpression = setter.ExpressionBody?.Expression as AssignmentExpressionSyntax;
            }

            if (assignmentExpression is not null && assignmentExpression.Right is IdentifierNameSyntax right &&
                right.Identifier.ValueText == "value")
            {
                return assignmentExpression.Left as IdentifierNameSyntax;
            }

            return null;
        }
    }
}
