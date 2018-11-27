// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
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
            return RazorSyntaxTree.Create(root, source, diagnostics, Options);
        }
    }
}
