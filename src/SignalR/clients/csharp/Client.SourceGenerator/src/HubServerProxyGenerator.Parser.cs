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

internal partial class HubServerProxyGenerator
{
    internal sealed class Parser
    {
        internal static bool IsSyntaxTargetForAttribute(SyntaxNode node) => node is AttributeSyntax
        {
            Name: IdentifierNameSyntax
            {
                Identifier:
                {
                    Text: "HubServerProxy"
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
            var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;

            if (attributeSymbol is null ||
                !attributeSymbol.ToString().EndsWith("HubServerProxyAttribute()", StringComparison.Ordinal))
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
                    DiagnosticDescriptors.HubServerProxyAttributedMethodIsNotPartial,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the method is an extension
            if (!symbol.IsExtensionMethod)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubServerProxyAttributedMethodIsNotExtension,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the method has one type parameter
            if (symbol.Arity != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubServerProxyAttributedMethodTypeArgCountIsBad,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the type parameter matches return type
            if (!SymbolEqualityComparer.Default.Equals(symbol.TypeArguments[0], symbol.ReturnType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubServerProxyAttributedMethodTypeArgAndReturnTypeDoesNotMatch,
                    symbol.Locations[0]));
                return false;
            }

            // Check that the method has correct parameters
            if (symbol.Parameters.Length != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubServerProxyAttributedMethodArgCountIsBad,
                    symbol.Locations[0]));
                return false;
            }
            var hubConnectionSymbol = symbol.Parameters[0].Type as INamedTypeSymbol;
            if (hubConnectionSymbol.ToString() != "Microsoft.AspNetCore.SignalR.Client.HubConnection")
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubServerProxyAttributedMethodArgIsNotHubConnection,
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
                    .EndsWith("HubServerProxyAttribute", StringComparison.Ordinal))
                {
                    continue;
                }

                return memberAccessExpressionSyntax;
            }

            return null;
        }

        private readonly SourceProductionContext _context;
        private readonly Compilation _compilation;

        internal Parser(SourceProductionContext context, Compilation compilation)
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
                        Diagnostic.Create(DiagnosticDescriptors.TooManyHubServerProxyAttributedMethods,
                        extraneous.GetLocation()));
                }

                // nothing to do
                return sourceGenerationSpec;
            }

            var methodDeclarationSyntax = methodDeclarationSyntaxes[0];

            var getProxySemanticModel = _compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
            var getProxyMethodSymbol = (IMethodSymbol)getProxySemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
            var getProxyClassSymbol = (INamedTypeSymbol)getProxyMethodSymbol.ContainingSymbol;

            // Populate spec with metadata on user-specific get proxy method and class
            if (!IsExtensionMethodSignatureValid(getProxyMethodSymbol, _context))
            {
                return sourceGenerationSpec;
            }
            if (!IsExtensionClassSignatureValid((ClassDeclarationSyntax)methodDeclarationSyntax.Parent))
            {
                return sourceGenerationSpec;
            }

            sourceGenerationSpec.GetterMethodAccessibility =
                GeneratorHelpers.GetAccessibilityString(getProxyMethodSymbol.DeclaredAccessibility);
            sourceGenerationSpec.GetterClassAccessibility =
                GeneratorHelpers.GetAccessibilityString(getProxyClassSymbol.DeclaredAccessibility);
            if (sourceGenerationSpec.GetterMethodAccessibility is null)
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.HubServerProxyAttributedMethodBadAccessibility,
                    methodDeclarationSyntax.GetLocation()));
                return sourceGenerationSpec;
            }
            sourceGenerationSpec.GetterMethodName = getProxyMethodSymbol.Name;
            sourceGenerationSpec.GetterClassName = getProxyClassSymbol.Name;
            sourceGenerationSpec.GetterNamespace = getProxyClassSymbol.ContainingNamespace.ToString();
            sourceGenerationSpec.GetterTypeParameterName = getProxyMethodSymbol.TypeParameters[0].Name;
            sourceGenerationSpec.GetterHubConnectionParameterName = getProxyMethodSymbol.Parameters[0].Name;

            var hubSymbols = new Dictionary<string, (ITypeSymbol, MemberAccessExpressionSyntax)>();

            // Go thru candidates and filter further
            foreach (var memberAccess in syntaxList)
            {
                var proxyType = ((GenericNameSyntax)memberAccess.Name).TypeArgumentList.Arguments[0];

                // Filter based on argument symbol
                var argumentModel = _compilation.GetSemanticModel(proxyType.SyntaxTree);
                if (ModelExtensions.GetSymbolInfo(argumentModel, proxyType).Symbol is not ITypeSymbol { IsAbstract: true } symbol)
                {
                    // T in GetProxy<T> must be an interface
                    _context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.HubServerProxyNonInterfaceGenericTypeArgument,
                        memberAccess.GetLocation(),
                        proxyType.ToString()));
                    continue;
                }

                // Receiver is a HubConnection and argument is abstract so save argument symbol for generation
                hubSymbols[symbol.Name] = (symbol, memberAccess);
            }

            // Generate spec for each proxy
            foreach (var (hubSymbol, memberAccess) in hubSymbols.Values)
            {
                var classSpec = new ClassSpec
                {
                    FullyQualifiedInterfaceTypeName = hubSymbol.ToString(),
                    ClassTypeName = $"Generated{hubSymbol.Name}",
                    CallSite = memberAccess.GetLocation()
                };

                var members = hubSymbol.GetMembers()
                    .Where(member => member.Kind == SymbolKind.Method)
                    .Select(member => (IMethodSymbol)member)
                    .Concat(hubSymbol.AllInterfaces.SelectMany(x => x
                        .GetMembers()
                        .Where(member => member.Kind == SymbolKind.Method)
                        .Select(member => (IMethodSymbol)member)));

                // Generate spec for each method
                foreach (var member in members)
                {
                    var methodSpec = new MethodSpec
                    {
                        Name = member.Name,
                        FullyQualifiedReturnTypeName = member.ReturnType.ToString()
                    };

                    if (member.ReturnType is INamedTypeSymbol { Arity: 1 } rtype)
                    {
                        methodSpec.InnerReturnTypeName = rtype.TypeArguments[0].ToString();
                    }

                    if (member.ReturnType is INamedTypeSymbol { Arity: 1, Name: "Task" } a
                        && a.TypeArguments[0] is INamedTypeSymbol { Arity: 1, Name: "ChannelReader" } b)
                    {
                        methodSpec.Stream = StreamSpec.ServerToClient & ~StreamSpec.AsyncEnumerable;
                        methodSpec.InnerReturnTypeName = b.TypeArguments[0].ToString();
                    }
                    else if (member.ReturnType is INamedTypeSymbol { Arity: 1, Name: "IAsyncEnumerable" } c)
                    {
                        methodSpec.Stream = StreamSpec.ServerToClient | StreamSpec.AsyncEnumerable;
                        methodSpec.InnerReturnTypeName = c.TypeArguments[0].ToString();
                    }
                    else
                    {
                        methodSpec.Stream = StreamSpec.None;
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

                        switch (parameter.Type)
                        {
                            case INamedTypeSymbol { Arity: 1, Name: "ChannelReader" }:
                                methodSpec.Stream |= StreamSpec.ClientToServer;
                                break;
                            case INamedTypeSymbol { Arity: 1, Name: "IAsyncEnumerable" }:
                                methodSpec.Stream |= StreamSpec.ClientToServer;
                                break;
                        }
                    }

                    // Validate return type
                    if (!methodSpec.Stream.HasFlag(StreamSpec.ServerToClient) &&
                        member.ReturnType is not INamedTypeSymbol { Name: "Task" or "ValueTask" })
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.HubServerProxyUnsupportedReturnType,
                                classSpec.CallSite,
                                methodSpec.Name, member.ReturnType.Name));
                        methodSpec.Support = SupportClassification.UnsupportedReturnType;
                        methodSpec.SupportHint = "Return type must be Task, ValueTask, Task<T> or ValueTask<T>";
                    }

                    classSpec.Methods.Add(methodSpec);
                }

                sourceGenerationSpec.Classes.Add(classSpec);
            }

            return sourceGenerationSpec;
        }
    }
}
