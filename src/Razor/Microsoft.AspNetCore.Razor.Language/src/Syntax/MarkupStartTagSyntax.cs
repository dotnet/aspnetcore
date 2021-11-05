// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal partial class MarkupStartTagSyntax
{
    public bool IsMarkupTransition
    {
        get
        {
            return ((InternalSyntax.MarkupStartTagSyntax)Green).IsMarkupTransition;
        }
    }

    public SyntaxList<RazorSyntaxNode> Children => GetLegacyChildren();

    public string GetTagNameWithOptionalBang()
    {
        return Name.IsMissing ? string.Empty : Bang?.Content + Name.Content;
    }

    public bool IsSelfClosing()
    {
        return ForwardSlash != null &&
            !ForwardSlash.IsMissing &&
            !CloseAngle.IsMissing;
    }

    public bool IsVoidElement()
    {
        return ParserHelpers.VoidElements.Contains(Name.Content);
    }

    private SyntaxList<RazorSyntaxNode> GetLegacyChildren()
    {
        // This method returns the children of this start tag in legacy format.
        // This is needed to generate the same classified spans as the legacy syntax tree.
        var builder = new SyntaxListBuilder(5);
        var tokens = SyntaxListBuilder<SyntaxToken>.Create();
        var context = this.GetSpanContext();

        // We want to know if this tag contains non-whitespace attribute content to set the appropriate AcceptedCharacters.
        // The prefix of a start tag(E.g '|<foo| attr>') will have 'Any' accepted characters if non-whitespace attribute content exists.
        var acceptsAnyContext = new SpanContext(context.ChunkGenerator, SpanEditHandler.CreateDefault());
        acceptsAnyContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.Any;
        var containsAttributesContent = false;
        foreach (var attribute in Attributes)
        {
            if (!string.IsNullOrWhiteSpace(attribute.GetContent()))
            {
                containsAttributesContent = true;
                break;
            }
        }

        if (!OpenAngle.IsMissing)
        {
            tokens.Add(OpenAngle);
        }
        if (Bang != null)
        {
            builder.Add(SyntaxFactory.MarkupTextLiteral(tokens.Consume()).WithSpanContext(acceptsAnyContext));

            tokens.Add(Bang);
            var acceptsNoneContext = new SpanContext(context.ChunkGenerator, SpanEditHandler.CreateDefault());
            acceptsNoneContext.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
            builder.Add(SyntaxFactory.RazorMetaCode(tokens.Consume()).WithSpanContext(acceptsNoneContext));
        }
        if (!Name.IsMissing)
        {
            tokens.Add(Name);
        }

        builder.Add(SyntaxFactory.MarkupTextLiteral(tokens.Consume()).WithSpanContext(containsAttributesContent ? acceptsAnyContext : context));

        builder.AddRange(Attributes);

        if (ForwardSlash != null)
        {
            tokens.Add(ForwardSlash);
        }
        if (!CloseAngle.IsMissing)
        {
            tokens.Add(CloseAngle);
        }

        if (tokens.Count > 0)
        {
            builder.Add(SyntaxFactory.MarkupTextLiteral(tokens.Consume()).WithSpanContext(context));
        }

        return new SyntaxList<RazorSyntaxNode>(builder.ToListNode().CreateRed(this, Position));
    }
}
