// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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

        public virtual RazorSyntaxTree Parse(RazorSourceDocument source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var chars = new char[source.Length];
            source.CopyTo(0, chars, 0, source.Length);

            var reader = new SeekableTextReader(chars, source.FileName);

            var context = new ParserContext(reader, Options.DesignTimeMode);

            var codeParser = new CSharpCodeParser(Options.Directives, context);
            var markupParser = new HtmlMarkupParser(context);

            codeParser.HtmlParser = markupParser;
            markupParser.CodeParser = codeParser;

            markupParser.ParseDocument();
            
            var root = context.Builder.Build();

            // Temporary code while we're still using legacy diagnostics in the SyntaxTree.
            var diagnostics = context.ErrorSink.Errors.Select(error => RazorDiagnostic.Create(error));

            return RazorSyntaxTree.Create(root, source, diagnostics, Options);
        }
    }
}
