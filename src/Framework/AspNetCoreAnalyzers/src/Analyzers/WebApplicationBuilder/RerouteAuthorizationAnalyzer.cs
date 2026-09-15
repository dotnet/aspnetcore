// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RerouteAuthorizationAnalyzer : DiagnosticAnalyzer
{
    internal const string FixKindKey = nameof(FixKindKey);
    internal const string InsertRoutingAfter = nameof(InsertRoutingAfter);
    internal const string MoveRerouterBeforeRouting = nameof(MoveRerouterBeforeRouting);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ExplicitRoutingRequiredForAuthorization,
            DiagnosticDescriptors.ReroutingMiddlewareMustPrecedeRouting);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            var webApplicationType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.WebApplication");
            var routingExtensionsType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.EndpointRoutingApplicationBuilderExtensions");
            var authorizationExtensionsType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.AuthorizationAppBuilderExtensions");
            var authorizationConventionExtensionsType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.AuthorizationEndpointConventionBuilderExtensions");
            var rewriteExtensionsType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.RewriteBuilderExtensions");
            var statusCodePagesExtensionsType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.StatusCodePagesExtensions");
            var exceptionHandlerExtensionsType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.ExceptionHandlerExtensions");

            if (webApplicationType is null ||
                routingExtensionsType is null ||
                authorizationExtensionsType is null ||
                authorizationConventionExtensionsType is null)
            {
                return;
            }

            context.RegisterOperationBlockStartAction(context =>
            {
                var invocations = new ConcurrentQueue<Invocation>();

                context.RegisterOperationAction(context =>
                {
                    var invocation = (IInvocationOperation)context.Operation;
                    if (!TryGetWebApplicationLocal(
                            invocation,
                            webApplicationType,
                            context.CancellationToken,
                            out var application) ||
                        !TryGetLinearContainer(invocation.Syntax, out var container))
                    {
                        return;
                    }

                    var targetMethod = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
                    var kind = GetInvocationKind(
                        targetMethod,
                        routingExtensionsType,
                        authorizationExtensionsType,
                        authorizationConventionExtensionsType,
                        rewriteExtensionsType,
                        statusCodePagesExtensionsType,
                        exceptionHandlerExtensionsType);

                    if (kind is not InvocationKind.Other)
                    {
                        invocations.Enqueue(new Invocation(application, container, invocation, kind));
                    }
                }, OperationKind.Invocation);

                context.RegisterOperationBlockEndAction(context => AnalyzeOperationBlock(context, invocations));
            });
        });
    }

    private static void AnalyzeOperationBlock(
        OperationBlockAnalysisContext context,
        ConcurrentQueue<Invocation> invocations)
    {
        var orderedInvocations = invocations.OrderBy(invocation => invocation.Operation.Syntax.SpanStart).ToArray();

        foreach (var rerouter in orderedInvocations.Where(invocation =>
            invocation.Kind is InvocationKind.Rewriter or InvocationKind.StatusCodeReExecute or InvocationKind.ExceptionHandlerReExecute))
        {
            var relatedInvocations = orderedInvocations
                .Where(invocation =>
                    SymbolEqualityComparer.Default.Equals(invocation.Application, rerouter.Application) &&
                    ReferenceEquals(invocation.Container, rerouter.Container))
                .ToArray();

            if (!relatedInvocations.Any(invocation => invocation.Kind is InvocationKind.UseAuthorization or InvocationKind.RequireAuthorization))
            {
                continue;
            }

            var routingInvocations = relatedInvocations
                .Where(invocation => invocation.Kind is InvocationKind.UseRouting)
                .ToArray();
            var rerouterName = rerouter.Operation.TargetMethod.Name;

            if (routingInvocations.Length == 0)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(FixKindKey, InsertRoutingAfter);
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ExplicitRoutingRequiredForAuthorization,
                    rerouter.Operation.Syntax.GetLocation(),
                    properties,
                    rerouterName));
                continue;
            }

            var precedingRouting = routingInvocations.LastOrDefault(
                invocation => invocation.Operation.Syntax.SpanStart < rerouter.Operation.Syntax.SpanStart);

            if (precedingRouting.Kind is InvocationKind.UseRouting)
            {
                var canMove = AreAdjacentStatements(precedingRouting.Operation.Syntax, rerouter.Operation.Syntax);
                var properties = canMove
                    ? ImmutableDictionary<string, string?>.Empty.Add(FixKindKey, MoveRerouterBeforeRouting)
                    : null;

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ReroutingMiddlewareMustPrecedeRouting,
                    rerouter.Operation.Syntax.GetLocation(),
                    properties,
                    rerouterName));
            }
        }
    }

    private static InvocationKind GetInvocationKind(
        IMethodSymbol method,
        INamedTypeSymbol routingExtensionsType,
        INamedTypeSymbol authorizationExtensionsType,
        INamedTypeSymbol authorizationConventionExtensionsType,
        INamedTypeSymbol? rewriteExtensionsType,
        INamedTypeSymbol? statusCodePagesExtensionsType,
        INamedTypeSymbol? exceptionHandlerExtensionsType)
    {
        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, routingExtensionsType) &&
            method.Name == "UseRouting")
        {
            return InvocationKind.UseRouting;
        }

        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, authorizationExtensionsType) &&
            method.Name == "UseAuthorization")
        {
            return InvocationKind.UseAuthorization;
        }

        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, authorizationConventionExtensionsType) &&
            method.Name == "RequireAuthorization")
        {
            return InvocationKind.RequireAuthorization;
        }

        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, rewriteExtensionsType) &&
            method.Name == "UseRewriter")
        {
            return InvocationKind.Rewriter;
        }

        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, statusCodePagesExtensionsType) &&
            method.Name == "UseStatusCodePagesWithReExecute")
        {
            return InvocationKind.StatusCodeReExecute;
        }

        if (SymbolEqualityComparer.Default.Equals(method.ContainingType, exceptionHandlerExtensionsType) &&
            method.Name == "UseExceptionHandler" &&
            method.Parameters.Any(parameter => parameter.Type.SpecialType is SpecialType.System_String))
        {
            return InvocationKind.ExceptionHandlerReExecute;
        }

        return InvocationKind.Other;
    }

    private static bool TryGetWebApplicationLocal(
        IInvocationOperation invocation,
        INamedTypeSymbol webApplicationType,
        CancellationToken cancellationToken,
        out ILocalSymbol application)
    {
        IOperation? receiver = invocation.Instance;
        if (receiver is null &&
            invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess })
        {
            receiver = invocation.SemanticModel?.GetOperation(memberAccess.Expression, cancellationToken);
        }

        if (TryFindWebApplicationLocal(receiver, webApplicationType, cancellationToken, out application))
        {
            return true;
        }

        application = null!;
        return false;
    }

    private static bool TryFindWebApplicationLocal(
        IOperation? operation,
        INamedTypeSymbol webApplicationType,
        CancellationToken cancellationToken,
        out ILocalSymbol application)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        if (operation is ILocalReferenceOperation localReference &&
            SymbolEqualityComparer.Default.Equals(localReference.Type, webApplicationType))
        {
            application = localReference.Local;
            return true;
        }

        if (operation is IInvocationOperation invocation)
        {
            IOperation? receiver = invocation.Instance;
            if (receiver is null &&
                invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess })
            {
                receiver = invocation.SemanticModel?.GetOperation(memberAccess.Expression, cancellationToken);
            }

            if (TryFindWebApplicationLocal(receiver, webApplicationType, cancellationToken, out application))
            {
                return true;
            }
        }

        application = null!;
        return false;
    }

    private static bool TryGetLinearContainer(SyntaxNode syntax, out SyntaxNode container)
    {
        var statement = syntax.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (statement?.Parent is GlobalStatementSyntax { Parent: CompilationUnitSyntax compilationUnit })
        {
            container = compilationUnit;
            return true;
        }

        if (statement?.Parent is BlockSyntax { Parent: BaseMethodDeclarationSyntax } block)
        {
            container = block;
            return true;
        }

        container = null!;
        return false;
    }

    private static bool AreAdjacentStatements(SyntaxNode firstInvocation, SyntaxNode secondInvocation)
    {
        var firstStatement = firstInvocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        var secondStatement = secondInvocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (firstStatement is null || secondStatement is null)
        {
            return false;
        }

        if (firstStatement.Parent is BlockSyntax block && ReferenceEquals(secondStatement.Parent, block))
        {
            return block.Statements.IndexOf(secondStatement) - block.Statements.IndexOf(firstStatement) == 1;
        }

        if (firstStatement.Parent is GlobalStatementSyntax firstGlobal &&
            secondStatement.Parent is GlobalStatementSyntax secondGlobal &&
            ReferenceEquals(firstGlobal.Parent, secondGlobal.Parent) &&
            firstGlobal.Parent is CompilationUnitSyntax compilationUnit)
        {
            return compilationUnit.Members.IndexOf(secondGlobal) - compilationUnit.Members.IndexOf(firstGlobal) == 1;
        }

        return false;
    }

    private enum InvocationKind
    {
        Other,
        UseRouting,
        UseAuthorization,
        RequireAuthorization,
        Rewriter,
        StatusCodeReExecute,
        ExceptionHandlerReExecute,
    }

    private readonly record struct Invocation(
        ILocalSymbol Application,
        SyntaxNode Container,
        IInvocationOperation Operation,
        InvocationKind Kind);
}
