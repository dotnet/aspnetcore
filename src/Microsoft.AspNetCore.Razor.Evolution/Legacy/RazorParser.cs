// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class RazorParser
    {
        public RazorParser()
            : this(RazorParserOptions.CreateDefaultOptions())
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

        public virtual RazorSyntaxTree Parse(TextReader input) => Parse(input.ReadToEnd());

        public virtual RazorSyntaxTree Parse(string input) => Parse(((ITextDocument)new SeekableTextReader(input)));

        public virtual RazorSyntaxTree Parse(char[] input) => Parse(((ITextDocument)new SeekableTextReader(input)));

        public virtual RazorSyntaxTree Parse(ITextDocument input) => ParseCore(input);

        private RazorSyntaxTree ParseCore(ITextDocument input)
        {
            var context = new ParserContext(input, Options.DesignTimeMode);

            var codeParser = new CSharpCodeParser(Options.Directives, context);
            var markupParser = new HtmlMarkupParser(context);

            codeParser.HtmlParser = markupParser;
            markupParser.CodeParser = codeParser;

            markupParser.ParseDocument();
            
            var root = context.Builder.Build();
            var diagnostics = context.ErrorSink.Errors;
            return RazorSyntaxTree.Create(root, diagnostics, Options);
        }
    }
}
