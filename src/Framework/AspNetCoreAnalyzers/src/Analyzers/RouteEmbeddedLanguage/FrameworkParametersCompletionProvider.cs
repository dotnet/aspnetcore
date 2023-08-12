// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Document = Microsoft.CodeAnalysis.Document;
using RoutePatternToken = Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax.EmbeddedSyntaxToken<Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern.RoutePatternKind>;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

using WellKnownType = WellKnownTypeData.WellKnownType;

[ExportCompletionProvider(nameof(RoutePatternCompletionProvider), LanguageNames.CSharp)]
[Shared]
public sealed class FrameworkParametersCompletionProvider : CompletionProvider
{
    private const string StartKey = nameof(StartKey);
    private const string LengthKey = nameof(LengthKey);
    private const string NewTextKey = nameof(NewTextKey);
    private const string NewPositionKey = nameof(NewPositionKey);
    private const string DescriptionKey = nameof(DescriptionKey);

    // Always soft-select these completion items.  Also, never filter down.
    private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(
        selectionBehavior: CompletionItemSelectionBehavior.SoftSelection,
        filterCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, Array.Empty<char>())));

    // The space between type and parameter name.
    // void TestMethod(int // <- space after type name
    public ImmutableHashSet<char> TriggerCharacters { get; } = ImmutableHashSet.Create(' ');

    public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
    {
        if (trigger.Kind is CompletionTriggerKind.Invoke or CompletionTriggerKind.InvokeAndCommitIfUnique)
        {
            return true;
        }

        if (trigger.Kind == CompletionTriggerKind.Insertion)
        {
            return TriggerCharacters.Contains(trigger.Character);
        }

        return false;
    }

    public override Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
    {
        if (!item.Properties.TryGetValue(DescriptionKey, out var description))
        {
            return Task.FromResult<CompletionDescription?>(null);
        }

        return Task.FromResult<CompletionDescription?>(CompletionDescription.Create(
            ImmutableArray.Create(new TaggedText(TextTags.Text, description))));
    }

    public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
        // These values have always been added by us.
        var startString = item.Properties[StartKey];
        var lengthString = item.Properties[LengthKey];
        var newText = item.Properties[NewTextKey];

        // This value is optionally added in some cases and may not always be there.
        item.Properties.TryGetValue(NewPositionKey, out var newPositionString);

        return Task.FromResult(CompletionChange.Create(
            new TextChange(new TextSpan(int.Parse(startString, CultureInfo.InvariantCulture), int.Parse(lengthString, CultureInfo.InvariantCulture)), newText),
            newPositionString == null ? null : int.Parse(newPositionString, CultureInfo.InvariantCulture)));
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var position = context.Position;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        SyntaxToken? parentOpt = null;

        var token = root.FindTokenOnLeftOfPosition(position);

        // If space is after ? or > then it's likely a nullable or generic type. Move to previous type token.
        if (token.IsKind(SyntaxKind.QuestionToken) || token.IsKind(SyntaxKind.GreaterThanToken))
        {
            token = token.GetPreviousToken();
        }

        // Whitespace should follow the identifier token of the parameter.
        if (!IsArgumentTypeToken(token))
        {
            return;
        }

        var container = TryFindMinimalApiArgument(token.Parent) ?? TryFindMvcActionParameter(token.Parent);
        if (container == null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        var wellKnownTypes = WellKnownTypes.GetOrCreate(semanticModel.Compilation);

        // Don't offer route parameter names when the parameter type can't be bound to route parameters.
        // e.g. special types like HttpContext, non-primitive types that don't have a static TryParse method.
        if (!IsCurrentParameterBindable(token, semanticModel, wellKnownTypes, context.CancellationToken))
        {
            return;
        }

        // Don't offer route parameter names when the parameter has an attribute that can't be bound to route parameters.
        // e.g [AsParameters] or [IFromBodyMetadata].
        var hasNonRouteAttribute = HasNonRouteAttribute(token, semanticModel, wellKnownTypes, context.CancellationToken);
        if (hasNonRouteAttribute)
        {
            return;
        }

        SyntaxToken routeStringToken;
        SyntaxNode methodNode;
        if (container.Parent.IsKind(SyntaxKind.Argument))
        {
            // Minimal API
            var mapMethodParts = RouteUsageDetector.FindMapMethodParts(semanticModel, wellKnownTypes, container, context.CancellationToken);
            if (mapMethodParts == null)
            {
                return;
            }
            var (_, routeStringExpression, delegateExpression) = mapMethodParts.Value;

            routeStringToken = routeStringExpression.Token;
            methodNode = delegateExpression;

            // Incomplete inline delegate syntax is very messy and arguments are mixed together.
            // Examine tokens to figure out wether the current token is the argument name.
            var previous = token.GetPreviousToken();
            if (previous.IsKind(SyntaxKind.CommaToken) ||
                previous.IsKind(SyntaxKind.OpenParenToken) ||
                previous.IsKind(SyntaxKind.OutKeyword) ||
                previous.IsKind(SyntaxKind.InKeyword) ||
                previous.IsKind(SyntaxKind.RefKeyword) ||
                previous.IsKind(SyntaxKind.ParamsKeyword) ||
                (previous.IsKind(SyntaxKind.CloseBracketToken) && previous.GetRequiredParent().FirstAncestorOrSelf<AttributeListSyntax>() != null) ||
                (previous.IsKind(SyntaxKind.LessThanToken) && previous.GetRequiredParent().FirstAncestorOrSelf<GenericNameSyntax>() != null))
            {
                // Positioned after type token. Don't replace current.
            }
            else
            {
                // Space after argument name. Don't show completion options.
                if (context.Trigger.Kind == CompletionTriggerKind.Insertion)
                {
                    return;
                }

                parentOpt = token;
            }
        }
        else if (container.IsKind(SyntaxKind.Parameter))
        {
            // MVC
            var methodSyntax = container.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodSyntax == null)
            {
                return;
            }

            var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax, context.CancellationToken);

            // Check method is a valid MVC action.
            if (methodSymbol?.ContainingType is not INamedTypeSymbol typeSymbol ||
                !MvcDetector.IsController(typeSymbol, wellKnownTypes) ||
                !MvcDetector.IsAction(methodSymbol, wellKnownTypes))
            {
                return;
            }

            var routeToken = TryGetMvcActionRouteToken(context, semanticModel, methodSyntax);
            if (routeToken == null)
            {
                return;
            }

            routeStringToken = routeToken.Value;
            methodNode = methodSyntax;

            if (((ParameterSyntax)container).Identifier == token)
            {
                // Space after argument name. Don't show completion options.
                if (context.Trigger.Kind == CompletionTriggerKind.Insertion)
                {
                    return;
                }

                parentOpt = token;
            }
        }
        else
        {
            return;
        }

        var routeUsageCache = RouteUsageCache.GetOrCreate(semanticModel.Compilation);
        var routeUsage = routeUsageCache.Get(routeStringToken, context.CancellationToken);
        if (routeUsage is null)
        {
            return;
        }

        var routePatternCompletionContext = new EmbeddedCompletionContext(context, routeUsage.RoutePattern);

        var existingParameterNames = GetExistingParameterNames(methodNode);
        foreach (var parameterName in existingParameterNames)
        {
            routePatternCompletionContext.AddUsedParameterName(parameterName);
        }

        ProvideCompletions(routePatternCompletionContext, parentOpt);

        if (routePatternCompletionContext.Items == null || routePatternCompletionContext.Items.Count == 0)
        {
            return;
        }

        foreach (var embeddedItem in routePatternCompletionContext.Items)
        {
            var change = embeddedItem.Change;
            var textChange = change.TextChange;

            var properties = ImmutableDictionary.CreateBuilder<string, string>();
            properties.Add(StartKey, textChange.Span.Start.ToString(CultureInfo.InvariantCulture));
            properties.Add(LengthKey, textChange.Span.Length.ToString(CultureInfo.InvariantCulture));
            properties.Add(NewTextKey, textChange.NewText ?? string.Empty);
            properties.Add(DescriptionKey, embeddedItem.FullDescription);

            if (change.NewPosition != null)
            {
                properties.Add(NewPositionKey, change.NewPosition.Value.ToString(CultureInfo.InvariantCulture));
            }

            // Keep everything sorted in the order we just produced the items in.
            var sortText = routePatternCompletionContext.Items.Count.ToString("0000", CultureInfo.InvariantCulture);
            context.AddItem(CompletionItem.Create(
                displayText: embeddedItem.DisplayText,
                inlineDescription: "",
                sortText: sortText,
                properties: properties.ToImmutable(),
                rules: s_rules,
                tags: ImmutableArray.Create(embeddedItem.Glyph)));
        }

        context.SuggestionModeItem = CompletionItem.Create(
            displayText: "<Name>",
            inlineDescription: "",
            rules: CompletionItemRules.Default);

        context.IsExclusive = true;
    }

    private static bool IsArgumentTypeToken(SyntaxToken token)
    {
        return SyntaxFacts.IsPredefinedType(token.Kind()) || token.IsKind(SyntaxKind.IdentifierToken);
    }

    private static SyntaxToken? TryGetMvcActionRouteToken(CompletionContext context, SemanticModel semanticModel, MethodDeclarationSyntax method)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.ArgumentList != null)
                {
                    foreach (var attributeArgument in attribute.ArgumentList.Arguments)
                    {
                        if (RouteStringSyntaxDetector.IsArgumentToAttributeParameterWithMatchingStringSyntaxAttribute(
                            semanticModel,
                            attributeArgument,
                            context.CancellationToken,
                            out var identifer) &&
                            identifer == "Route" &&
                            attributeArgument.Expression is LiteralExpressionSyntax literalExpression)
                        {
                            return literalExpression.Token;
                        }
                    }
                }
            }
        }

        return null;
    }

    private static SyntaxNode? TryFindMvcActionParameter(SyntaxNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current.IsKind(SyntaxKind.Parameter))
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    private static SyntaxNode? TryFindMinimalApiArgument(SyntaxNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current.Parent?.IsKind(SyntaxKind.Argument) ?? false)
            {
                if (current.Parent?.Parent?.IsKind(SyntaxKind.ArgumentList) ?? false)
                {
                    return current;
                }
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool HasNonRouteAttribute(SyntaxToken token, SemanticModel semanticModel, WellKnownTypes wellKnownTypes, CancellationToken cancellationToken)
    {
        if (token.Parent?.Parent is ParameterSyntax parameter)
        {
            foreach (var attributeList in parameter.AttributeLists.OfType<AttributeListSyntax>())
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeTypeSymbol = semanticModel.GetSymbolInfo(attribute, cancellationToken).GetAnySymbol();

                    if (attributeTypeSymbol != null)
                    {
                        if (attributeTypeSymbol.ContainingSymbol is ITypeSymbol typeSymbol &&
                            wellKnownTypes.Implements(typeSymbol, RouteWellKnownTypes.NonRouteMetadataTypes))
                        {
                            return true;
                        }

                        if (SymbolEqualityComparer.Default.Equals(attributeTypeSymbol.ContainingSymbol, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_AsParametersAttribute)))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static bool IsCurrentParameterBindable(SyntaxToken token, SemanticModel semanticModel, WellKnownTypes wellKnownTypes, CancellationToken cancellationToken)
    {
        if (token.Parent.IsKind(SyntaxKind.PredefinedType))
        {
            return true;
        }

        var parameterTypeSymbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).GetAnySymbol();
        if (parameterTypeSymbol is INamedTypeSymbol typeSymbol)
        {
            return ParsabilityHelper.GetParsability(typeSymbol, wellKnownTypes) == Parsability.Parsable;

        }
        else if (parameterTypeSymbol is IMethodSymbol)
        {
            // If the parameter type is a method then the method is bound to the minimal API.
            return false;
        }

        // The cursor is on an identifier (parameter name) and completion is explicitly triggered (e.g. CTRL+SPACE)
        return true;
    }

    private static ImmutableArray<string> GetExistingParameterNames(SyntaxNode node)
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        if (node is TupleExpressionSyntax tupleExpression)
        {
            foreach (var argument in tupleExpression.Arguments)
            {
                if (argument.Expression is DeclarationExpressionSyntax declarationExpression &&
                    declarationExpression.Designation is SingleVariableDesignationSyntax variableDesignationSyntax &&
                    variableDesignationSyntax.Identifier is { IsMissing: false } identifer)
                {
                    builder.Add(identifer.ValueText);
                }
            }
        }
        else
        {
            var parameterList = node switch
            {
                ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpression => parenthesizedLambdaExpression.ParameterList,
                MethodDeclarationSyntax methodDeclaration => methodDeclaration.ParameterList,
                _ => null
            };

            if (parameterList != null)
            {
                foreach (var p in parameterList.Parameters)
                {
                    if (p is ParameterSyntax parameter &&
                        parameter.Identifier is { IsMissing: false } identifer)
                    {
                        builder.Add(identifer.ValueText);
                    }
                }
            }
        }

        return builder.ToImmutable();
    }

    private static void ProvideCompletions(EmbeddedCompletionContext context, SyntaxToken? parentOpt)
    {
        foreach (var routeParameter in context.Tree.RouteParameters)
        {
            context.AddIfMissing(routeParameter.Name, suffix: string.Empty, description: "(Route parameter)", WellKnownTags.Parameter, parentOpt);
        }
    }

    private readonly struct RoutePatternItem
    {
        public readonly string DisplayText;
        public readonly string InlineDescription;
        public readonly string FullDescription;
        public readonly string Glyph;
        public readonly CompletionChange Change;

        public RoutePatternItem(
            string displayText, string inlineDescription, string fullDescription, string glyph, CompletionChange change)
        {
            DisplayText = displayText;
            InlineDescription = inlineDescription;
            FullDescription = fullDescription;
            Glyph = glyph;
            Change = change;
        }
    }

    private readonly struct EmbeddedCompletionContext
    {
        private readonly CompletionContext _context;
        private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);

        public readonly RoutePatternTree Tree;
        public readonly CancellationToken CancellationToken;
        public readonly int Position;
        public readonly CompletionTrigger Trigger;
        public readonly List<RoutePatternItem> Items = new();
        public readonly CompletionListSpanContainer CompletionListSpan = new();

        internal class CompletionListSpanContainer
        {
            public TextSpan? Value { get; set; }
        }

        public EmbeddedCompletionContext(
            CompletionContext context,
            RoutePatternTree tree)
        {
            _context = context;
            Tree = tree;
            Position = _context.Position;
            Trigger = _context.Trigger;
            CancellationToken = _context.CancellationToken;
        }

        public void AddUsedParameterName(string name)
        {
            _names.Add(name);
        }

        public void AddIfMissing(
            string displayText, string suffix, string description, string glyph,
            SyntaxToken? parentOpt, int? positionOffset = null, string? insertionText = null)
        {
            var replacementStart = parentOpt != null
                ? parentOpt.Value.GetLocation().SourceSpan.Start
                : Position;
            var replacementEnd = parentOpt != null
                ? parentOpt.Value.GetLocation().SourceSpan.End
                : Position;

            var replacementSpan = TextSpan.FromBounds(replacementStart, replacementEnd);
            var newPosition = replacementStart + positionOffset;

            insertionText ??= displayText;

            AddIfMissing(new RoutePatternItem(
                displayText, suffix, description, glyph,
                CompletionChange.Create(
                    new TextChange(replacementSpan, insertionText),
                    newPosition)));
        }

        public void AddIfMissing(RoutePatternItem item)
        {
            if (_names.Add(item.DisplayText))
            {
                Items.Add(item);
            }
        }

        public static string EscapeText(string text, SyntaxToken token)
        {
            // This function is called when Completion needs to escape something its going to
            // insert into the user's string token.  This means that we only have to escape
            // things that completion could insert.  In this case, the only regex character
            // that is relevant is the \ character, and it's only relevant if we insert into
            // a normal string and not a verbatim string.  There are no other regex characters
            // that completion will produce that need any escaping.
            Debug.Assert(token.IsKind(SyntaxKind.StringLiteralToken));
            return token.IsVerbatimStringLiteral()
                ? text
                : text.Replace(@"\", @"\\");
        }
    }
}
