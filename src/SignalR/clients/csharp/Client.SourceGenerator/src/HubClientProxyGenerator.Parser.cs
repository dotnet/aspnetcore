// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator;

internal sealed partial class HubClientProxyGenerator
{
    public sealed class Parser
    {
        internal static bool IsSyntaxTargetForAttribute(SyntaxNode node) => node is AttributeSyntax
        {
            Name: IdentifierNameSyntax
            {
                Identifier:
                {
                    Text: "HubClientProxy"
                }
            },
            Parent:
            {
                Parent: MethodDeclarationSyntax
                {
                    Parent: ClassDeclarationSyntax
                }
            }
        };

        internal static MethodDeclarationSyntax? GetSemanticTargetForAttribute(GeneratorSyntaxContext context)
        {
            var attributeSyntax = (AttributeSyntax)context.Node;
            var attributeSymbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol;

            if (attributeSymbol is null ||
                !attributeSymbol.ToString().EndsWith("HubClientProxyAttribute()", StringComparison.Ordinal))
            {
                return null;
            }

            return (MethodDeclarationSyntax)attributeSyntax.Parent.Parent;
        }

        private static bool IsExtensionMethodSignatureValid(IMethodSymbol symbol, SourceProductionContext context)
        {
            // Check that the method is partial
            if (!symbol.IsPartialDefinition)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubClientProxyAttributedMethodIsNotPartial,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the method is an extension
            if (!symbol.IsExtensionMethod)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubClientProxyAttributedMethodIsNotExtension,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the method has one type parameter
            if (symbol.Arity != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubClientProxyAttributedMethodTypeArgCountIsBad,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the method has correct parameters
            if (symbol.Parameters.Length != 2)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubClientProxyAttributedMethodArgCountIsBad,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the type parameter matches 2nd parameter type
            if (!SymbolEqualityComparer.Default.Equals(symbol.TypeArguments[0], symbol.Parameters[1].Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubClientProxyAttributedMethodTypeArgAndProviderTypeDoesNotMatch,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the type parameter matches 2nd parameter type
            if (symbol.ReturnType.ToString() != "System.IDisposable")
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubClientProxyAttributedMethodHasBadReturnType,
                    symbol.Locations[0]));
                return false;
            }

            var hubConnectionSymbol = symbol.Parameters[0].Type as INamedTypeSymbol;
            if (hubConnectionSymbol.ToString() != "Microsoft.AspNetCore.SignalR.Client.HubConnection")
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubClientProxyAttributedMethodArgIsNotHubConnection,
                    symbol.Locations[0]));
                return false;
            }

            return true;
        }

        private static bool IsExtensionClassSignatureValid(ClassDeclarationSyntax syntax)
        {
            // Check partialness
            var hasPartialModifier = false;
            foreach (var modifier in syntax.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PartialKeyword))
                {
                    hasPartialModifier = true;
                }
            }
            if (!hasPartialModifier)
            {
                return false;
            }

            return true;
        }

        internal static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is MemberAccessExpressionSyntax
        {
            Name: GenericNameSyntax
            {
                Arity: 1
            }
        };

        internal static MemberAccessExpressionSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var memberAccessExpressionSyntax = (MemberAccessExpressionSyntax)context.Node;

            if (ModelExtensions.GetSymbolInfo(context.SemanticModel, memberAccessExpressionSyntax).Symbol is not IMethodSymbol
                methodSymbol)
            {
                return null;
            }

            if (!methodSymbol.IsExtensionMethod)
            {
                return null;
            }

            foreach (var attributeData in methodSymbol.GetAttributes())
            {
                if (!attributeData.AttributeClass.ToString()
                    .EndsWith("HubClientProxyAttribute", StringComparison.Ordinal))
                {
                    continue;
                }

                return memberAccessExpressionSyntax;
            }

            return null;
        }

        private readonly SourceProductionContext _context;
        private readonly Compilation _compilation;

        public Parser(SourceProductionContext context, Compilation compilation)
        {
            _context = context;
            _compilation = compilation;
        }

        internal SourceGenerationSpec Parse(ImmutableArray<MethodDeclarationSyntax> methodDeclarationSyntaxes, ImmutableArray<MemberAccessExpressionSyntax> syntaxList)
        {
            // Source generation spec will be populated by type specs for each hub type.
            // Type specs themselves are populated by method specs which are populated by argument specs.
            // Source generation spec is then used by emitter to actually generate source.
            var sourceGenerationSpec = new SourceGenerationSpec();

            // There must be exactly one attributed method
            if (methodDeclarationSyntaxes.Length != 1)
            {
                // Report diagnostic for each attributed method when there are many
                foreach (var extraneous in methodDeclarationSyntaxes)
                {
                    _context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.TooManyHubClientProxyAttributedMethods,
                            extraneous.GetLocation()));
                }

                // nothing to do
                return sourceGenerationSpec;
            }

            var methodDeclarationSyntax = methodDeclarationSyntaxes[0];

            var registerCallbackProviderSemanticModel = _compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
            var registerCallbackProviderMethodSymbol = (IMethodSymbol)registerCallbackProviderSemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
            var registerCallbackProviderClassSymbol = (INamedTypeSymbol)registerCallbackProviderMethodSymbol.ContainingSymbol;

            // Populate spec with metadata on user-specific get proxy method and class
            if (!IsExtensionMethodSignatureValid(registerCallbackProviderMethodSymbol, _context))
            {
                return sourceGenerationSpec;
            }
            if (!IsExtensionClassSignatureValid((ClassDeclarationSyntax)methodDeclarationSyntax.Parent))
            {
                return sourceGenerationSpec;
            }

            sourceGenerationSpec.SetterMethodAccessibility =
                GeneratorHelpers.GetAccessibilityString(registerCallbackProviderMethodSymbol.DeclaredAccessibility);
            sourceGenerationSpec.SetterClassAccessibility =
                GeneratorHelpers.GetAccessibilityString(registerCallbackProviderClassSymbol.DeclaredAccessibility);
            if (sourceGenerationSpec.SetterMethodAccessibility is null)
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubClientProxyAttributedMethodBadAccessibility,
                    methodDeclarationSyntax.GetLocation()));
                return sourceGenerationSpec;
            }
            sourceGenerationSpec.SetterMethodName = registerCallbackProviderMethodSymbol.Name;
            sourceGenerationSpec.SetterClassName = registerCallbackProviderClassSymbol.Name;
            sourceGenerationSpec.SetterNamespace = registerCallbackProviderClassSymbol.ContainingNamespace.ToString();
            sourceGenerationSpec.SetterTypeParameterName = registerCallbackProviderMethodSymbol.TypeParameters[0].Name;
            sourceGenerationSpec.SetterHubConnectionParameterName = registerCallbackProviderMethodSymbol.Parameters[0].Name;
            sourceGenerationSpec.SetterProviderParameterName = registerCallbackProviderMethodSymbol.Parameters[1].Name;

            var providerSymbols = new Dictionary<string, (ITypeSymbol, MemberAccessExpressionSyntax)>();

            // Go thru candidates and filter further
            foreach (var memberAccess in syntaxList)
            {
                // Extract type symbol
                ITypeSymbol symbol;
                if (memberAccess.Name is GenericNameSyntax { Arity: 1 } gns)
                {
                    // Method is using generic syntax so the sole generic arg is the type
                    var argType = gns.TypeArgumentList.Arguments[0];
                    var argModel = _compilation.GetSemanticModel(argType.SyntaxTree);
                    symbol = (ITypeSymbol)argModel.GetSymbolInfo(argType).Symbol;
                }
                else if (memberAccess.Name is not GenericNameSyntax
                         && memberAccess.Parent.ChildNodes().FirstOrDefault(x => x is ArgumentListSyntax) is
                             ArgumentListSyntax
                         { Arguments: { Count: 1 } } als)
                {
                    // Method isn't using generic syntax so inspect first expression in arguments to deduce the type
                    var argModel = _compilation.GetSemanticModel(als.Arguments[0].Expression.SyntaxTree);
                    var argTypeInfo = argModel.GetTypeInfo(als.Arguments[0].Expression);
                    symbol = argTypeInfo.Type;
                }
                else
                {
                    // If we are here then candidate has different number of args than we expect so we skip
                    continue;
                }

                // Receiver is a HubConnection, so save argument symbol for generation
                providerSymbols[symbol.Name] = (symbol, memberAccess);
            }

            // Generate spec for each provider
            foreach (var (providerSymbol, memberAccess) in providerSymbols.Values)
            {
                var typeSpec = new TypeSpec
                {
                    FullyQualifiedTypeName = providerSymbol.ToString(),
                    TypeName = providerSymbol.Name,
                    CallSite = memberAccess.GetLocation()
                };

                var members = providerSymbol.GetMembers()
                    .Where(member => member.Kind == SymbolKind.Method)
                    .Select(member => (IMethodSymbol)member)
                    .Union<IMethodSymbol>(providerSymbol.AllInterfaces.SelectMany(x => x
                        .GetMembers()
                        .Where(member => member.Kind == SymbolKind.Method)
                        .Select(member => (IMethodSymbol)member)), SymbolEqualityComparer.Default).ToList();

                // Generate spec for each method
                foreach (var member in members)
                {
                    var methodSpec = new MethodSpec
                    {
                        Name = member.Name
                    };

                    // Validate return type
                    if (!(member.ReturnsVoid || member.ReturnType is INamedTypeSymbol { Arity: 0, Name: "Task" }))
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.HubClientProxyUnsupportedReturnType,
                            typeSpec.CallSite,
                            methodSpec.Name, member.ReturnType.Name));
                        methodSpec.Support = SupportClassification.UnsupportedReturnType;
                        methodSpec.SupportHint = "Return type must be void or Task";
                    }

                    // Generate spec for each argument
                    foreach (var parameter in member.Parameters)
                    {
                        var argumentSpec = new ArgumentSpec
                        {
                            Name = parameter.Name,
                            FullyQualifiedTypeName = parameter.Type.ToString()
                        };

                        methodSpec.Arguments.Add(argumentSpec);
                    }

                    typeSpec.Methods.Add(methodSpec);
                }

                sourceGenerationSpec.Types.Add(typeSpec);
            }

            return sourceGenerationSpec;
        }
    }
}
