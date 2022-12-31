// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class RouteParameterUnusedParameterFixer : CodeFixProvider
{
    private static readonly TypeSyntax DefaultType = SyntaxFactory.ParseTypeName("string");

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.RoutePatternUnusedParameter.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        var routeUsageCache = RouteUsageCache.GetOrCreate(semanticModel.Compilation);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Properties.TryGetValue("RouteParameterName", out var routeParameterName))
            {
                context.RegisterCodeFix(
                    CodeAction.Create($"Add parameter '{routeParameterName}'",
                        cancellationToken => AddRouteParameterAsync(diagnostic, root, routeUsageCache, context.Document, cancellationToken),
                        equivalenceKey: DiagnosticDescriptors.RoutePatternUnusedParameter.Id),
                    diagnostic);
            }
        }
    }

    private static Task<Document> AddRouteParameterAsync(Diagnostic diagnostic, SyntaxNode root, RouteUsageCache routeUsageCache, Document document, CancellationToken cancellationToken)
    {
        var param = root.FindNode(diagnostic.Location.SourceSpan);

        var token = param.GetFirstToken();
        var routeUsage = routeUsageCache.Get(token, cancellationToken);

        // Check that the route is used in a context with a method, e.g. attribute on an action or Map method.
        if (routeUsage?.UsageContext.MethodSyntax == null)
        {
            return Task.FromResult(document);
        }

        return Task.FromResult(UpdateDocument(diagnostic, root, document, routeUsage.UsageContext.MethodSyntax));
    }

    private static Document UpdateDocument(Diagnostic diagnostic, SyntaxNode root, Document document, SyntaxNode methodSyntax)
    {
        var routeParameterName = diagnostic.Properties["RouteParameterName"];
        var routeParameterPolicy = diagnostic.Properties["RouteParameterPolicy"];
        var routeParameterIsOptional = Convert.ToBoolean(diagnostic.Properties["RouteParameterIsOptional"], CultureInfo.InvariantCulture);
        var routeParameterInsertIndex = Convert.ToInt32(diagnostic.Properties["RouteParameterInsertIndex"], CultureInfo.InvariantCulture);

        var resolvedType = CalculateTypeFromPolicy(routeParameterPolicy);
        if (routeParameterIsOptional)
        {
            resolvedType = SyntaxFactory.NullableType(resolvedType);
        }

        // After fix, navigate to type with CodeAction_Navigation.
        var type = resolvedType.WithAdditionalAnnotations(new SyntaxAnnotation("CodeAction_Navigation"));
        var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(routeParameterName!)).WithType(type);
        var updatedMethod = methodSyntax switch
        {
            BaseMethodDeclarationSyntax declaredMethodSyntax => AddParameter(declaredMethodSyntax, newParameter, routeParameterInsertIndex),
            ParenthesizedLambdaExpressionSyntax lambdaExpressionSyntax => AddParameter(lambdaExpressionSyntax, newParameter, routeParameterInsertIndex),
            _ => throw new InvalidOperationException($"Unexpected method syntax: {methodSyntax.GetType().FullName}")
        };

        // Update document.
        var updatedSyntaxTree = root.ReplaceNode(methodSyntax, updatedMethod);
        return document.WithSyntaxRoot(updatedSyntaxTree);
    }

    private static SyntaxNode AddParameter(BaseMethodDeclarationSyntax syntax, ParameterSyntax parameterSyntax, int parameterIndex)
    {
        if (parameterIndex == -1)
        {
            parameterIndex = 0;
        }

        var newParameters = syntax.ParameterList.Parameters.Insert(parameterIndex, parameterSyntax);
        return syntax.WithParameterList(syntax.ParameterList.WithParameters(newParameters));
    }

    private static SyntaxNode AddParameter(ParenthesizedLambdaExpressionSyntax syntax, ParameterSyntax parameterSyntax, int parameterIndex)
    {
        if (parameterIndex == -1)
        {
            parameterIndex = 0;
        }

        var newParameters = syntax.ParameterList.Parameters.Insert(parameterIndex, parameterSyntax);
        return syntax.WithParameterList(syntax.ParameterList.WithParameters(newParameters));
    }

    private static TypeSyntax CalculateTypeFromPolicy(string? routeParameterPolicy)
    {
        if (routeParameterPolicy == null)
        {
            return DefaultType;
        }

        string? resolvedName = null;
        foreach (var policy in routeParameterPolicy.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
        {
            // Match policy to a type.
            var typeName = policy switch
            {
                "int" => "int",
                "long" => "long",
                "bool" => "bool",
                "datetime" => "System.DateTime",
                "decimal" => "System.Decimal",
                "double" => "double",
                "float" => "float",
                "guid" => "System.Guid",
                _ => null
            };

            if (typeName != null)
            {
                // Route has conflicting policies, e.g. int and decimal. Default to string.
                if (resolvedName != null && typeName != resolvedName)
                {
                    return DefaultType;
                }

                resolvedName = typeName;
            }
        }

        return resolvedName != null ? SyntaxFactory.ParseTypeName(resolvedName) : DefaultType;
    }
}
