// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

/// <summary>
/// This completion provider expands the completion list of target symbols defined in the
/// ExtensionMethodsCache to include extension methods that can be invoked on the target
/// type that are defined in auxillary packages. This completion provider is designed to be
/// used in conjunction with the `AddPackageFixer` to recommend adding the missing packages
/// extension methods are defined in.
/// </summary>
[ExportCompletionProvider(nameof(ExtensionMethodsCompletionProvider), LanguageNames.CSharp)]
[Shared]
public sealed class ExtensionMethodsCompletionProvider : CompletionProvider
{
    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        if (!context.Document.SupportsSemanticModel)
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var span = context.CompletionListSpan;
        var token = root.FindToken(span.Start);
        if (token.Parent == null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        var wellKnownTypes = WellKnownTypes.GetOrCreate(semanticModel.Compilation);
        var wellKnownExtensionMethodCache = ExtensionMethodsCache.ConstructFromWellKnownTypes(wellKnownTypes);

        // We find the nearest member access expression to the adjacent expression to resolve the
        // target type of the extension method that the user is invoking. For example, `app.` should
        // allow us to resolve to a `WebApplication` instance and `builder.Services.Add` should resolve
        // to an `IServiceCollection`.
        var nearestMemberAccessExpression = FindNearestMemberAccessExpression(token.Parent);
        if (nearestMemberAccessExpression is not null && nearestMemberAccessExpression is MemberAccessExpressionSyntax memberAccess)
        {
            var symbol = semanticModel.GetSymbolInfo(memberAccess.Expression);
            var symbolType = symbol.Symbol switch
            {
                IMethodSymbol methodSymbol => methodSymbol.ReturnType,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                ILocalSymbol localSymbol => localSymbol.Type,
                _ => null
            };

            var matchingExtensionMethods = wellKnownExtensionMethodCache.Where(pair => IsMatchingExtensionMethod(pair, symbolType, token));
            foreach (var item in matchingExtensionMethods)
            {
                context.CompletionListSpan = span;
                context.AddItem(CompletionItem.Create(
                    displayText: item.Key.ExtensionMethod,
                    sortText: item.Key.ExtensionMethod,
                    filterText: item.Key.ExtensionMethod
                ));
            }
        }
    }

    private static SyntaxNode? FindNearestMemberAccessExpression(SyntaxNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current?.IsKind(SyntaxKind.SimpleMemberAccessExpression) ?? false)
            {
                return current;
            }

            current = current?.Parent;
        }

        return null;
    }

    private static bool IsMatchingExtensionMethod(
        KeyValuePair<ThisAndExtensionMethod, PackageSourceAndNamespace> pair,
        ISymbol? symbolType,
        SyntaxToken token)
    {
        if (symbolType is null)
        {
            return false;
        }

        // If the token that we are parsing is some sort of identifier, this indicates that the user
        // has triggered a completion with characters already inserted into the invocation (e.g. `builder.Services.Ad$$).
        // In this case, we only want to provide completions that match the characters that have been inserted.
        var isIdentifierToken = token.IsKind(SyntaxKind.IdentifierName) || token.IsKind(SyntaxKind.IdentifierToken);
        return SymbolEqualityComparer.Default.Equals(pair.Key.ThisType, symbolType) &&
            (!isIdentifierToken || pair.Key.ExtensionMethod.Contains(token.ValueText));
    }
}
