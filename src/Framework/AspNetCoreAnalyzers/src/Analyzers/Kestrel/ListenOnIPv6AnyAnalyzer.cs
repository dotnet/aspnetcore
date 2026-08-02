// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Microsoft.AspNetCore.Analyzers.Kestrel;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ListenOnIPv6AnyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(KestrelServerOptionsListenInvocation, SyntaxKind.InvocationExpression);
    }

    private void KestrelServerOptionsListenInvocation(SyntaxNodeAnalysisContext context)
    {
        // fail fast before accessing SemanticModel
        if (context.Node is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name: IdentifierNameSyntax { Identifier.ValueText: "Listen" }
                }
            } kestrelOptionsListenExpressionSyntax)
        {
            return;
        }

        var nodeOperation = context.SemanticModel.GetOperation(context.Node, context.CancellationToken);
        if (!IsKestrelServerOptionsType(nodeOperation, out var kestrelOptionsListenInvocation))
        {
            return;
        }

        var addressArgument = kestrelOptionsListenInvocation?.Arguments.FirstOrDefault();
        if (!IsIPAddressType(addressArgument?.Parameter))
        {
            return;
        }

        var args = kestrelOptionsListenExpressionSyntax.ArgumentList;
        var ipAddressArgumentSyntax = args.Arguments.FirstOrDefault();
        if (ipAddressArgumentSyntax is null)
        {
            return;
        }

        // explicit usage like `options.Listen(IPAddress.Any, ...)`
        if (ipAddressArgumentSyntax is ArgumentSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name: IdentifierNameSyntax { Identifier.ValueText: "Any" }
            }
        })
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny, ipAddressArgumentSyntax.GetLocation()));
        }

        // usage via local variable like
        // ```
        // var myIp = IPAddress.Any;
        // options.Listen(myIp, ...);
        // ```
        if (addressArgument!.Value is ILocalReferenceOperation localReferenceOperation)
        {
            var localVariableDeclaration = localReferenceOperation.Local.DeclaringSyntaxReferences.FirstOrDefault();
            if (localVariableDeclaration is null)
            {
                return;
            }

            var localVarSyntax = localVariableDeclaration.GetSyntax(context.CancellationToken);
            if (localVarSyntax is VariableDeclaratorSyntax
            {
                Initializer.Value: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "Any"
                }
            })
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny, ipAddressArgumentSyntax.GetLocation()));
            }
        }
    }

    private static bool IsIPAddressType(IParameterSymbol? parameter) => parameter is 
    {
        Type: // searching type `System.Net.IPAddress`
        {
            Name: "IPAddress",
            ContainingNamespace: { Name: "Net", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } }
        }
    };

    private static bool IsKestrelServerOptionsType(IOperation? operation, out IInvocationOperation? kestrelOptionsListenInvocation)
    {
        var result = operation is IInvocationOperation // searching type `Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions`
        {
            TargetMethod: { Name: "Listen" },
            Instance.Type:
            {
                Name: "KestrelServerOptions",
                ContainingNamespace:
                {
                    Name: "Core",
                    ContainingNamespace:
                    {
                        Name: "Kestrel",
                        ContainingNamespace:
                        {
                            Name: "Server",
                            ContainingNamespace:
                            {
                                Name: "AspNetCore",
                                ContainingNamespace:
                                {
                                    Name: "Microsoft",
                                    ContainingNamespace.IsGlobalNamespace: true
                                }
                            }
                        }
                    }
                }
            }
        };

        kestrelOptionsListenInvocation = result ? (IInvocationOperation)operation! : null;
        return result;
    }
}
