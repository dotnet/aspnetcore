// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    internal partial class HubProxyGenerator
    {
        internal class Parser
        {
            internal static bool IsSyntaxTargetForAttribute(SyntaxNode node) => node is AttributeSyntax
            {
                Name: IdentifierNameSyntax
                {
                    Identifier:
                    {
                        Text: "GetProxy"
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
                    attributeSymbol.ToString() != "Microsoft.AspNetCore.SignalR.Client.GetProxyAttribute.GetProxyAttribute()")
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
                        DiagnosticDescriptors.HubProxyGetProxyAttributedMethodIsNotPartial,
                        symbol.Locations[0]));
                    return false;
                }

                // Check that the method is an extension
                if (!symbol.IsExtensionMethod)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.HubProxyGetProxyAttributedMethodIsNotExtension,
                        symbol.Locations[0]));
                    return false;
                }

                // Check that the method has one type parameter
                if (symbol.Arity != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.HubProxyGetProxyAttributedMethodTypeArgCountIsBad,
                        symbol.Locations[0]));
                    return false;
                }

                // Check that the type parameter matches return type
                if (!SymbolEqualityComparer.Default.Equals(symbol.TypeArguments[0], symbol.ReturnType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.HubProxyGetProxyAttributedMethodTypeArgAndReturnTypeDoesNotMatch,
                        symbol.Locations[0]));
                    return false;
                }

                // Check that the method has correct parameters
                if (symbol.Parameters.Length != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.HubProxyGetProxyAttributedMethodArgCountIsBad,
                        symbol.Locations[0]));
                    return false;
                }
                var hubConnectionSymbol = symbol.Parameters[0].Type as INamedTypeSymbol;
                if (hubConnectionSymbol.ToString() != "Microsoft.AspNetCore.SignalR.Client.HubConnection")
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.HubProxyGetProxyAttributedMethodArgIsNotHubConnection,
                        symbol.Locations[0]));
                    return false;
                }

                return true;
            }

            private static bool IsExtensionClassSignatureValid(ClassDeclarationSyntax syntax, SourceProductionContext context)
            {
                // Check partialness
                var hasPartialModifier = false;
                foreach (var modifier in syntax.Modifiers)
                {
                    if (modifier.Kind() == SyntaxKind.PartialKeyword)
                    {
                        hasPartialModifier = true;
                    }
                }
                if (!hasPartialModifier)
                {
                    // TODO: Emit diagnostic report
                    return false;
                }

                return true;
            }

            internal static MethodDeclarationSyntax? GetSoleDeclarationSyntax(
                ImmutableArray<MethodDeclarationSyntax?> syntax)
            {
                if (syntax.Length > 1)
                {
                    // TODO: Emit diagnostic report
                }
                else if (syntax.Length == 1)
                {
                    return syntax[0];
                }

                return null;
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
                    if (attributeData.AttributeClass.ToString() != "Microsoft.AspNetCore.SignalR.Client.GetProxyAttribute")
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

            private static string GetAccessibilityString(Accessibility accessibility)
            {
                switch (accessibility)
                {
                    case Accessibility.Private:
                        return "private";
                    case Accessibility.ProtectedAndInternal:
                        return "protected internal";
                    case Accessibility.Protected:
                        return "protected";
                    case Accessibility.Internal:
                        return "internal";
                    case Accessibility.Public:
                        return "public";
                    default:
                        return null;
                }
            }

            internal SourceGenerationSpec Parse(MethodDeclarationSyntax? methodDeclarationSyntax, ImmutableArray<MemberAccessExpressionSyntax> syntaxList)
            {
                // Source generation spec will be populated by type specs for each hub type.
                // Type specs themselves are populated by method specs which are populated by argument specs.
                // Source generation spec is then used by emitter to actually generate source.
                var sourceGenerationSpec = new SourceGenerationSpec();

                if (methodDeclarationSyntax is null)
                {
                    // nothing to do
                    return sourceGenerationSpec;
                }

                var compilation = _compilation;

                var getProxySemanticModel = compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
                var getProxyMethodSymbol = (IMethodSymbol)getProxySemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                var getProxyClassSymbol = (INamedTypeSymbol)getProxyMethodSymbol.ContainingSymbol;

                // Populate spec with metadata on user-specific get proxy method and class
                if (!IsExtensionMethodSignatureValid(getProxyMethodSymbol, _context))
                {
                    return sourceGenerationSpec;
                }
                if (!IsExtensionClassSignatureValid((ClassDeclarationSyntax)methodDeclarationSyntax.Parent, _context))
                {
                    return sourceGenerationSpec;
                }

                sourceGenerationSpec.GetProxyMethodAccessibility =
                    GetAccessibilityString(getProxyMethodSymbol.DeclaredAccessibility);
                sourceGenerationSpec.GetProxyClassAccessibility =
                    GetAccessibilityString(getProxyClassSymbol.DeclaredAccessibility);
                if (sourceGenerationSpec.GetProxyMethodAccessibility is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.HubProxyGetProxyAttributedMethodBadAccessibility,
                        methodDeclarationSyntax.GetLocation()));
                    return sourceGenerationSpec;
                }
                sourceGenerationSpec.GetProxyMethodName = getProxyMethodSymbol.Name;
                sourceGenerationSpec.GetProxyClassName = getProxyClassSymbol.Name;
                sourceGenerationSpec.GetProxyNamespace = getProxyClassSymbol.ContainingNamespace.ToString();
                sourceGenerationSpec.GetProxyTypeParameterName = getProxyMethodSymbol.TypeParameters[0].Name;
                sourceGenerationSpec.GetProxyHubConnectionParameterName = getProxyMethodSymbol.Parameters[0].Name;

                var hubSymbols = new Dictionary<string, (ITypeSymbol, MemberAccessExpressionSyntax)>();

                // Go thru candidates and filter further
                foreach (var memberAccess in syntaxList)
                {
                    var proxyType = ((GenericNameSyntax)memberAccess.Name).TypeArgumentList.Arguments[0];

                    // Filter based on argument symbol
                    var argumentModel = compilation.GetSemanticModel(proxyType.SyntaxTree);
                    if (ModelExtensions.GetSymbolInfo(argumentModel, proxyType).Symbol is not ITypeSymbol { IsAbstract: true } symbol)
                    {
                        // T in GetProxy<T> must be an interface
                        _context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.HubProxyNonInterfaceGenericTypeArgument,
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
                    var classSpec = new ClassSpec();
                    classSpec.InterfaceTypeName = hubSymbol.Name;
                    classSpec.FullyQualifiedInterfaceTypeName = hubSymbol.ToString();
                    classSpec.ClassTypeName = $"Generated{hubSymbol.Name}";
                    classSpec.CallSite = memberAccess.GetLocation();

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
                                    DiagnosticDescriptors.HubProxyUnsupportedReturnType,
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
}
