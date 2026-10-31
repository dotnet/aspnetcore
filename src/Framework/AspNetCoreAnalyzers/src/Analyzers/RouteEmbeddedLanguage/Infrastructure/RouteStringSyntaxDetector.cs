// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class RouteStringSyntaxDetector
{
    private static readonly EmbeddedLanguageCommentDetector _commentDetector = new(ImmutableArray.Create("Route"));

    public static bool IsRouteStringSyntaxToken(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken, out RouteOptions options)
    {
        options = default;

        if (!IsAnyStringLiteral(token.RawKind))
        {
            return false;
        }

        if (!TryGetStringFormat(token, semanticModel, cancellationToken, out var identifier, out var stringOptions))
        {
            return false;
        }

        if (identifier != "Route")
        {
            return false;
        }

        if (stringOptions != null)
        {
            return EmbeddedLanguageCommentOptions<RouteOptions>.TryGetOptions(stringOptions, out options);
        }

        return true;
    }

    private static bool IsAnyStringLiteral(int rawKind)
    {
        return rawKind == (int)SyntaxKind.StringLiteralToken ||
               rawKind == (int)SyntaxKind.SingleLineRawStringLiteralToken ||
               rawKind == (int)SyntaxKind.MultiLineRawStringLiteralToken ||
               rawKind == (int)SyntaxKind.Utf8StringLiteralToken ||
               rawKind == (int)SyntaxKind.Utf8SingleLineRawStringLiteralToken ||
               rawKind == (int)SyntaxKind.Utf8MultiLineRawStringLiteralToken;
    }

    private static bool TryGetStringFormat(
        SyntaxToken token,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier,
        out IEnumerable<string>? options)
    {
        options = null;

        if (token.Parent is not LiteralExpressionSyntax)
        {
            identifier = null;
            return false;
        }

        if (HasLanguageComment(token, out identifier, out options))
        {
            return true;
        }

        var container = token.TryFindContainer();
        if (container is null)
        {
            identifier = null;
            return false;
        }

        if (container.Parent.IsKind(SyntaxKind.Argument))
        {
            if (IsArgumentWithMatchingStringSyntaxAttribute(semanticModel, container.Parent, cancellationToken, out identifier))
            {
                return true;
            }
        }
        else if (container.Parent.IsKind(SyntaxKind.AttributeArgument))
        {
            if (IsArgumentToAttributeParameterWithMatchingStringSyntaxAttribute(semanticModel, container.Parent, cancellationToken, out identifier))
            {
                return true;
            }
        }
        else
        {
            var statement = container.FirstAncestorOrSelf<SyntaxNode>(n => n is StatementSyntax);
            if (statement.IsSimpleAssignmentStatement())
            {
                GetPartsOfAssignmentStatement(statement, out var left, out var right);
                if (container == right &&
                    IsFieldOrPropertyWithMatchingStringSyntaxAttribute(
                        semanticModel, left, cancellationToken, out identifier))
                {
                    return true;
                }
            }

            if (container.Parent?.IsKind(SyntaxKind.EqualsValueClause) ?? false)
            {
                if (container.Parent.Parent?.IsKind(SyntaxKind.VariableDeclarator) ?? false)
                {
                    var variableDeclarator = container.Parent.Parent;
                    var symbol =
                        semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken) ??
                        semanticModel.GetDeclaredSymbol(GetIdentifierOfVariableDeclarator(variableDeclarator).GetRequiredParent(), cancellationToken);

                    if (IsFieldOrPropertyWithMatchingStringSyntaxAttribute(symbol, out identifier))
                    {
                        return true;
                    }
                }
                else if (IsEqualsValueOfPropertyDeclaration(container.Parent))
                {
                    var property = container.Parent.GetRequiredParent();
                    var symbol = semanticModel.GetDeclaredSymbol(property, cancellationToken);

                    if (IsFieldOrPropertyWithMatchingStringSyntaxAttribute(symbol, out identifier))
                    {
                        return true;
                    }
                }
            }
        }

        identifier = null;
        return false;
    }

    private static bool HasLanguageComment(
        SyntaxToken token,
        [NotNullWhen(true)] out string? identifier,
        [NotNullWhen(true)] out IEnumerable<string>? options)
    {
        if (HasLanguageComment(token.GetPreviousToken().TrailingTrivia, out identifier, out options))
        {
            return true;
        }

        // Check for the common case of a string literal in a large binary expression.  For example `"..." + "..." +
        // "..."` We never want to consider these as regex/json tokens as processing them would require knowing the
        // contents of every string literal, and having our lexers/parsers somehow stitch them all together.  This is
        // beyond what those systems support (and would only work for constant strings anyways).  This prevents both
        // incorrect results *and* avoids heavy perf hits walking up large binary expressions (often while a caller is
        // themselves walking down such a large expression).
        if (token.Parent.IsLiteralExpression() &&
            token.Parent.Parent.IsBinaryExpression() &&
            token.Parent.Parent.RawKind == (int)SyntaxKind.AddExpression)
        {
            return false;
        }

        for (var node = token.Parent; node != null; node = node.Parent)
        {
            if (HasLanguageComment(node.GetLeadingTrivia(), out identifier, out options))
            {
                return true;
            }
            // Stop walking up once we hit a statement.  We don't need/want statements higher up the parent chain to
            // have any impact on this token.
            if (IsStatement(node))
            {
                break;
            }
        }

        return false;
    }

    private static bool HasLanguageComment(
        SyntaxTriviaList list,
        [NotNullWhen(true)] out string? identifier,
        [NotNullWhen(true)] out IEnumerable<string>? options)
    {
        foreach (var trivia in list)
        {
            if (HasLanguageComment(trivia, out identifier, out options))
            {
                return true;
            }
        }

        identifier = null;
        options = null;
        return false;
    }

    private static bool HasLanguageComment(
        SyntaxTrivia trivia,
        [NotNullWhen(true)] out string? identifier,
        [NotNullWhen(true)] out IEnumerable<string>? options)
    {
        if (IsRegularComment(trivia))
        {
            // Note: ToString on SyntaxTrivia is non-allocating.  It will just return the
            // underlying text that the trivia is already pointing to.
            var text = trivia.ToString();
            if (_commentDetector.TryMatch(text, out identifier, out options))
            {
                return true;
            }
        }

        identifier = null;
        options = null;
        return false;
    }

    public static bool IsStatement([NotNullWhen(true)] SyntaxNode? node)
       => node is StatementSyntax;

    public static bool IsRegularComment(this SyntaxTrivia trivia)
        => trivia.IsSingleOrMultiLineComment() || trivia.IsShebangDirective();

    public static bool IsSingleOrMultiLineComment(this SyntaxTrivia trivia)
        => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia);

    public static bool IsShebangDirective(this SyntaxTrivia trivia)
        => trivia.IsKind(SyntaxKind.ShebangDirectiveTrivia);

    public static bool IsEqualsValueOfPropertyDeclaration(SyntaxNode? node)
        => node?.Parent is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.Initializer == node;

    private static SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node)
        => ((VariableDeclaratorSyntax)node).Identifier;

    private static bool IsFieldOrPropertyWithMatchingStringSyntaxAttribute(
        SemanticModel semanticModel,
        SyntaxNode left,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier)
    {
        var symbol = semanticModel.GetSymbolInfo(left, cancellationToken).Symbol;
        return IsFieldOrPropertyWithMatchingStringSyntaxAttribute(symbol, out identifier);
    }

    public static void GetPartsOfAssignmentStatement(
        SyntaxNode statement, out SyntaxNode left, out SyntaxNode right)
    {
        GetPartsOfAssignmentExpressionOrStatement(
            ((ExpressionStatementSyntax)statement).Expression, out left, out _, out right);
    }

    public static void GetPartsOfAssignmentExpressionOrStatement(
        SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
    {
        var expression = statement;
        if (statement is ExpressionStatementSyntax expressionStatement)
        {
            expression = expressionStatement.Expression;
        }

        var assignment = (AssignmentExpressionSyntax)expression;
        left = assignment.Left;
        operatorToken = assignment.OperatorToken;
        right = assignment.Right;
    }

    private static bool IsArgumentWithMatchingStringSyntaxAttribute(
        SemanticModel semanticModel,
        SyntaxNode argument,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier)
    {
        var parameter = FindParameterForArgument(semanticModel, argument, allowUncertainCandidates: true, cancellationToken);
        return HasMatchingStringSyntaxAttribute(parameter, out identifier);
    }

    public static bool IsArgumentToAttributeParameterWithMatchingStringSyntaxAttribute(
        SemanticModel semanticModel,
        SyntaxNode argument,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out string? identifier)
    {
        // First, see if this is an `X = "..."` argument that is binding to a field/prop on the attribute.
        var fieldOrProperty = FindFieldOrPropertyForAttributeArgument(semanticModel, argument, cancellationToken);
        if (fieldOrProperty != null)
        {
            return HasMatchingStringSyntaxAttribute(fieldOrProperty, out identifier);
        }

        // Otherwise, see if it's a normal named/position argument to the attribute.
        var parameter = FindParameterForAttributeArgument(semanticModel, argument, allowUncertainCandidates: true, cancellationToken);
        return HasMatchingStringSyntaxAttribute(parameter, out identifier);
    }

    public static bool IsFieldOrPropertyWithMatchingStringSyntaxAttribute(
        ISymbol? symbol, [NotNullWhen(true)] out string? identifier)
    {
        identifier = null;
        return symbol is IFieldSymbol or IPropertySymbol &&
            HasMatchingStringSyntaxAttribute(symbol, out identifier);
    }

    public static bool HasMatchingStringSyntaxAttribute(
        [NotNullWhen(true)] ISymbol? symbol,
        [NotNullWhen(true)] out string? identifier)
    {
        if (symbol != null)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (IsMatchingStringSyntaxAttribute(attribute, out identifier))
                {
                    return true;
                }
            }
        }

        identifier = null;
        return false;
    }

    private static bool IsMatchingStringSyntaxAttribute(
        AttributeData attribute,
        [NotNullWhen(true)] out string? identifier)
    {
        identifier = null;
        if (attribute.ConstructorArguments.Length == 0)
        {
            return false;
        }

        if (attribute.AttributeClass is not
            {
                Name: "StringSyntaxAttribute",
                ContainingNamespace:
                {
                    Name: nameof(CodeAnalysis),
                    ContainingNamespace:
                    {
                        Name: nameof(System.Diagnostics),
                        ContainingNamespace:
                        {
                            Name: nameof(System),
                            ContainingNamespace.IsGlobalNamespace: true,
                        }
                    }
                }
            })
        {
            return false;
        }

        var argument = attribute.ConstructorArguments[0];
        if (argument.Kind != TypedConstantKind.Primitive || argument.Value is not string argString)
        {
            return false;
        }

        identifier = argString;
        return true;
    }

    private static ISymbol? FindFieldOrPropertyForAttributeArgument(SemanticModel semanticModel, SyntaxNode argument, CancellationToken cancellationToken)
        => argument is AttributeArgumentSyntax { NameEquals.Name: var name }
            ? semanticModel.GetSymbolInfo(name, cancellationToken).GetAnySymbol()
            : null;

    private static IParameterSymbol? FindParameterForArgument(SemanticModel semanticModel, SyntaxNode argument, bool allowUncertainCandidates, CancellationToken cancellationToken)
        => ((ArgumentSyntax)argument).DetermineParameter(semanticModel, allowUncertainCandidates, allowParams: false, cancellationToken);

    private static IParameterSymbol? FindParameterForAttributeArgument(SemanticModel semanticModel, SyntaxNode argument, bool allowUncertainCandidates, CancellationToken cancellationToken)
        => ((AttributeArgumentSyntax)argument).DetermineParameter(semanticModel, allowUncertainCandidates, allowParams: false, cancellationToken);

    /// <summary>
    /// Returns the parameter to which this argument is passed. If <paramref name="allowParams"/>
    /// is true, the last parameter will be returned if it is params parameter and the index of
    /// the specified argument is greater than the number of parameters.
    /// </summary>
    public static IParameterSymbol? DetermineParameter(
        this ArgumentSyntax argument,
        SemanticModel semanticModel,
        bool allowUncertainCandidates = false,
        bool allowParams = false,
        CancellationToken cancellationToken = default)
    {
        if (argument.Parent is not BaseArgumentListSyntax argumentList ||
            argumentList.Parent is null)
        {
            return null;
        }

        // Get the symbol as long if it's not null or if there is only one candidate symbol
        var symbolInfo = semanticModel.GetSymbolInfo(argumentList.Parent, cancellationToken);
        var symbols = GetBestOrAllSymbols(symbolInfo);

        if (symbols.Length >= 2 && !allowUncertainCandidates)
        {
            return null;
        }

        foreach (var symbol in symbols)
        {
            var parameters = symbol.GetParameters();

            // Handle named argument
            if (argument.NameColon != null && !argument.NameColon.IsMissing)
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                var parameter = parameters.FirstOrDefault(p => p.Name == name);
                if (parameter != null)
                {
                    return parameter;
                }

                continue;
            }

            // Handle positional argument
            var index = argumentList.Arguments.IndexOf(argument);
            if (index < 0)
            {
                continue;
            }

            if (index < parameters.Length)
            {
                return parameters[index];
            }

            if (allowParams)
            {
                var lastParameter = parameters.LastOrDefault();
                if (lastParameter == null)
                {
                    continue;
                }

                if (lastParameter.IsParams)
                {
                    return lastParameter;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the parameter to which this argument is passed. If <paramref name="allowParams"/>
    /// is true, the last parameter will be returned if it is params parameter and the index of
    /// the specified argument is greater than the number of parameters.
    /// </summary>
    /// <remarks>
    /// Returns null if the <paramref name="argument"/> is a named argument.
    /// </remarks>
    public static IParameterSymbol? DetermineParameter(
        this AttributeArgumentSyntax argument,
        SemanticModel semanticModel,
        bool allowUncertainCandidates = false,
        bool allowParams = false,
        CancellationToken cancellationToken = default)
    {
        // if argument is a named argument it can't map to a parameter.
        if (argument.NameEquals != null)
        {
            return null;
        }
        if (argument.Parent is not AttributeArgumentListSyntax argumentList)
        {
            return null;
        }
        if (argumentList.Parent is not AttributeSyntax invocableExpression)
        {
            return null;
        }
        var symbols = GetBestOrAllSymbols(semanticModel.GetSymbolInfo(invocableExpression, cancellationToken));
        if (symbols.Length >= 2 && !allowUncertainCandidates)
        {
            return null;
        }
        foreach (var symbol in symbols)
        {
            var parameters = symbol.GetParameters();

            // Handle named argument
            if (argument.NameColon != null && !argument.NameColon.IsMissing)
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                var parameter = parameters.FirstOrDefault(p => p.Name == name);
                if (parameter != null)
                {
                    return parameter;
                }
                continue;
            }

            // Handle positional argument
            var index = argumentList.Arguments.IndexOf(argument);
            if (index < 0)
            {
                continue;
            }
            if (index < parameters.Length)
            {
                return parameters[index];
            }
            if (allowParams)
            {
                var lastParameter = parameters.LastOrDefault();
                if (lastParameter == null)
                {
                    continue;
                }
                if (lastParameter.IsParams)
                {
                    return lastParameter;
                }
            }
        }

        return null;
    }

    public static ImmutableArray<ISymbol> GetBestOrAllSymbols(SymbolInfo info)
    {
        if (info.Symbol != null)
        {
            return ImmutableArray.Create(info.Symbol);
        }
        else if (info.CandidateSymbols.Length > 0)
        {
            return info.CandidateSymbols;
        }

        return ImmutableArray<ISymbol>.Empty;
    }
}
