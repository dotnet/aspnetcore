// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    internal partial class HubProxyGenerator
    {
        public class Parser
        {
            private readonly GeneratorExecutionContext _context;

            public Parser(GeneratorExecutionContext context)
            {
                _context = context;
            }

            /// <param name="syntaxList">A filtered list of candidates</param>
            public SourceGenerationSpec Parse(List<MemberAccessExpressionSyntax> syntaxList)
            {
                // Source generation spec will be populated by type specs for each hub type.
                // Type specs themselves are populated by method specs which are populated by argument specs.
                // Source generation spec is then used by emitter to actually generate source.
                var sourceGenerationSpec = new SourceGenerationSpec();
                var compilation = _context.Compilation;

                var hubSymbols = new Dictionary<string, (ITypeSymbol, MemberAccessExpressionSyntax)>();
                var iHubConnectionType =
                    compilation.GetTypeByMetadataName("Microsoft.AspNetCore.SignalR.Client.HubConnection");

                // Go thru candidates and filter further
                foreach (var memberAccess in syntaxList)
                {
                    var expressionModel = compilation.GetSemanticModel(memberAccess.Expression.SyntaxTree);
                    var typeInfo = expressionModel.GetTypeInfo(memberAccess.Expression);

                    // Filter based on receiver symbol
                    if (!SymbolEqualityComparer.Default.Equals(typeInfo.Type, iHubConnectionType) &&
                        (typeInfo.Type.AllInterfaces.IsDefaultOrEmpty ||
                         !typeInfo.Type.AllInterfaces.Any(x =>
                             SymbolEqualityComparer.Default.Equals(x, iHubConnectionType))))
                    {
                        // Member access is not acting on HubConnection or a type implementing it; as such we will skip
                        continue;
                    }

                    var proxyType = ((GenericNameSyntax)memberAccess.Name).TypeArgumentList.Arguments[0];

                    // Filter based on argument symbol
                    var argumentModel = compilation.GetSemanticModel(proxyType.SyntaxTree);
                    if (argumentModel.GetSymbolInfo(proxyType).Symbol is not ITypeSymbol { IsAbstract: true } symbol)
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
