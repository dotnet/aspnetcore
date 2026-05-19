// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using RoutePatternToken = Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax.EmbeddedSyntaxToken<Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern.RoutePatternKind>;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[ExportCompletionProvider(nameof(RoutePatternCompletionProvider), LanguageNames.CSharp)]
[Shared]
public class RoutePatternCompletionProvider : CompletionProvider
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

    public ImmutableHashSet<char> TriggerCharacters { get; } = ImmutableHashSet.Create(
        ':', // policy name
        '{'); // parameter name

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
        if (context.Trigger.Kind is not CompletionTriggerKind.Invoke and
            not CompletionTriggerKind.InvokeAndCommitIfUnique and
            not CompletionTriggerKind.Insertion)
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var stringToken = root.FindToken(context.Position);
        if (context.Position <= stringToken.SpanStart ||
            context.Position >= stringToken.Span.End)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return;
        }

        var routeUsageCache = RouteUsageCache.GetOrCreate(semanticModel.Compilation);
        var routeUsage = routeUsageCache.Get(stringToken, context.CancellationToken);
        if (routeUsage is null)
        {
            return;
        }

        var routePatternCompletionContext = new EmbeddedCompletionContext(
            context,
            routeUsage,
            stringToken);
        ProvideCompletions(routePatternCompletionContext);

        if (routePatternCompletionContext.Items.Count == 0)
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

        if (routePatternCompletionContext.CompletionListSpan.Value != null)
        {
            context.CompletionListSpan = routePatternCompletionContext.CompletionListSpan.Value.Value;
        }
        context.IsExclusive = true;
    }

    private static void ProvideCompletions(EmbeddedCompletionContext context)
    {
        var result = GetCurrentToken(context);
        if (result == null)
        {
            return;
        }

        var (node, token) = result.Value;

        // First, act as if the user just inserted the previous character.  This will cause us
        // to complete down to the set of relevant items based on that character. If we get
        // anything, we're done and can just show the user those items.  If we have no items to
        // add *and* the user was explicitly invoking completion, then just add the entire set
        // of suggestions to help the user out.
        switch (token.Kind)
        {
            case RoutePatternKind.ColonToken:
                ProvidePolicyNameCompletions(context, parentOpt: null);
                break;
            case RoutePatternKind.OpenBraceToken:
                ProvideParameterCompletions(context, parentOpt: null);
                break;
        }

        if (context.Items.Count > 0)
        {
            // We added items.  Nothing else to do here.
            return;
        }

        if (context.Trigger.Kind == CompletionTriggerKind.Insertion)
        {
            // The user was typing a character, and we had nothing to add for them.  Just bail
            // out immediately as we cannot help in this circumstance.
            return;
        }

        // We added no items, but the user explicitly asked for completion.  Add all the
        // items we can to help them out.
        switch (token.Kind)
        {
            case RoutePatternKind.PolicyFragmentToken:
                context.CompletionListSpan.Value = token.GetSpan();
                ProvidePolicyNameCompletions(context, node);
                break;
            case RoutePatternKind.ParameterNameToken:
                context.CompletionListSpan.Value = token.GetSpan();
                ProvideParameterCompletions(context, node);
                break;
        }
    }

    private static (RoutePatternNode Parent, RoutePatternToken Token)? GetCurrentToken(EmbeddedCompletionContext context)
    {
        var previousVirtualCharOpt = context.RouteUsage.RoutePattern.Text.Find(context.Position - 1);
        if (previousVirtualCharOpt == null)
        {
            // We didn't have a previous character.  Can't determine the set of 
            // regex items to show.
            return null;
        }

        var previousVirtualChar = previousVirtualCharOpt.Value;
        return FindToken(context.RouteUsage.RoutePattern.Root, previousVirtualChar);
    }

    private static void ProvideParameterCompletions(EmbeddedCompletionContext context, RoutePatternNode? parentOpt)
    {
        if (context.RouteUsage.UsageContext.MethodSymbol != null)
        {
            foreach (var parameterSymbol in context.RouteUsage.UsageContext.ResolvedParameters)
            {
                // Don't suggest parameter name if it already exists in the route.
                if (!context.RouteUsage.RoutePattern.TryGetRouteParameter(parameterSymbol.RouteParameterName, out _))
                {
                    context.AddIfMissing(parameterSymbol.RouteParameterName, suffix: null, description: null, WellKnownTags.Parameter, parentOpt: parentOpt);
                }
            }
        }
    }

    private static void ProvidePolicyNameCompletions(EmbeddedCompletionContext context, RoutePatternNode? parentOpt)
    {
        // Provide completions depending upon the route type.
        // Blazor: https://learn.microsoft.com/aspnet/core/blazor/fundamentals/routing
        // HTTP: https://learn.microsoft.com/aspnet/core/fundamentals/routing

        context.AddIfMissing("int", suffix: null, "Matches any 32-bit integer.", WellKnownTags.Keyword, parentOpt);
        context.AddIfMissing("bool", suffix: null, "Matches true or false. Case-insensitive.", WellKnownTags.Keyword, parentOpt);
        context.AddIfMissing("datetime", suffix: null, "Matches a valid DateTime value in the invariant culture.", WellKnownTags.Keyword, parentOpt);
        context.AddIfMissing("decimal", suffix: null, "Matches a valid decimal value in the invariant culture.", WellKnownTags.Keyword, parentOpt);
        context.AddIfMissing("double", suffix: null, "Matches a valid double value in the invariant culture.", WellKnownTags.Keyword, parentOpt);
        context.AddIfMissing("float", suffix: null, "Matches a valid float value in the invariant culture.", WellKnownTags.Keyword, parentOpt);
        context.AddIfMissing("guid", suffix: null, "Matches a valid Guid value.", WellKnownTags.Keyword, parentOpt);
        context.AddIfMissing("long", suffix: null, "Matches any 64-bit integer.", WellKnownTags.Keyword, parentOpt);

        // The following constraints are only available for HTTP route matching.
        if (context.RouteUsage.UsageContext.UsageType != RouteUsageType.Component)
        {
            context.AddIfMissing("minlength", suffix: null, "Matches a string with a length greater than, or equal to, the constraint argument.", WellKnownTags.Keyword, parentOpt);
            context.AddIfMissing("maxlength", suffix: null, "Matches a string with a length less than, or equal to, the constraint argument.", WellKnownTags.Keyword, parentOpt);
            context.AddIfMissing("length", suffix: null, @"The string length constraint supports one or two constraint arguments.

If there is one argument the string length must equal the argument. For example, length(10) matches a string with exactly 10 characters.

If there are two arguments then the string length must be greater than, or equal to, the first argument and less than, or equal to, the second argument. For example, length(8,16) matches a string at least 8 and no more than 16 characters long.", WellKnownTags.Keyword, parentOpt);
            context.AddIfMissing("min", suffix: null, "Matches an integer with a value greater than, or equal to, the constraint argument.", WellKnownTags.Keyword, parentOpt);
            context.AddIfMissing("max", suffix: null, "Matches an integer with a value less than, or equal to, the constraint argument.", WellKnownTags.Keyword, parentOpt);
            context.AddIfMissing("range", suffix: null, "Matches an integer with a value greater than, or equal to, the first constraint argument and less than, or equal to, the second constraint argument.", WellKnownTags.Keyword, parentOpt);
            context.AddIfMissing("alpha", suffix: null, "Matches a string that contains only lowercase or uppercase letters A through Z in the English alphabet.", WellKnownTags.Keyword, parentOpt);
            context.AddIfMissing("regex", suffix: null, "Matches a string to the regular expression constraint argument.", WellKnownTags.Keyword, parentOpt);
            context.AddIfMissing("required", suffix: null, "Used to enforce that a non-parameter value is present during URL generation.", WellKnownTags.Keyword, parentOpt);
        }
    }

    private static (RoutePatternNode Parent, RoutePatternToken Token)? FindToken(RoutePatternNode parent, VirtualChar ch)
    {
        foreach (var child in parent)
        {
            if (child.IsNode)
            {
                var result = FindToken(child.Node, ch);
                if (result != null)
                {
                    return result;
                }
            }
            else
            {
                if (child.Token.VirtualChars.Contains(ch))
                {
                    return (parent, child.Token);
                }
            }
        }

        return null;
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
        private readonly HashSet<string> _names = new();

        public readonly RouteUsageModel RouteUsage;
        public readonly SyntaxToken StringToken;
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
            RouteUsageModel routeUsage,
            SyntaxToken stringToken)
        {
            _context = context;
            RouteUsage = routeUsage;
            StringToken = stringToken;
            Position = _context.Position;
            Trigger = _context.Trigger;
            CancellationToken = _context.CancellationToken;
        }

        public void AddIfMissing(
            string displayText, string? suffix, string? description, string glyph,
            RoutePatternNode? parentOpt, int? positionOffset = null, string? insertionText = null)
        {
            var replacementStart = parentOpt != null
                ? parentOpt.GetSpan().Start
                : Position;
            var replacementEnd = parentOpt != null
                ? parentOpt.GetSpan().End
                : Position;

            var replacementSpan = TextSpan.FromBounds(replacementStart, replacementEnd);
            var newPosition = replacementStart + positionOffset;

            insertionText ??= displayText;
            var escapedInsertionText = EscapeText(insertionText, StringToken);

            if (escapedInsertionText != insertionText)
            {
                newPosition += escapedInsertionText.Length - insertionText.Length;
            }

            AddIfMissing(new RoutePatternItem(
                displayText, suffix ?? string.Empty, description ?? string.Empty, glyph,
                CompletionChange.Create(
                    new TextChange(replacementSpan, escapedInsertionText),
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
