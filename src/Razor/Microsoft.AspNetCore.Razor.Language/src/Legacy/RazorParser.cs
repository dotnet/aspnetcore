// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class RazorParser
{
    public RazorParser()
        : this(RazorParserOptions.CreateDefault())
    {
    }

    public RazorParser(RazorParserOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        Options = options;
    }

    public RazorParserOptions Options { get; }

    public virtual RazorSyntaxTree Parse(RazorSourceDocument source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var context = new ParserContext(source, Options);
        var codeParser = new CSharpCodeParser(Options.Directives, context);
        var markupParser = new HtmlMarkupParser(context);

        codeParser.HtmlParser = markupParser;
        markupParser.CodeParser = codeParser;

        var diagnostics = context.ErrorSink.Errors;

        var root = markupParser.ParseDocument().CreateRed();

        var syntaxTree = RazorSyntaxTree.Create(root, source, diagnostics, Options);
        return syntaxTree;
    }
}
