// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class RazorParser
    {
        public RazorParser()
        {
        }

        public bool DesignTimeMode { get; set; }

        public virtual RazorSyntaxTree Parse(TextReader input)
        {
            var reader = new SeekableTextReader(input);

            return Parse((ITextDocument)reader);
        }

        public virtual RazorSyntaxTree Parse(ITextDocument input)
        {
            return ParseCore(input);
        }

        private RazorSyntaxTree ParseCore(ITextDocument input)
        {
            var context = new ParserContext(input, DesignTimeMode);

            var codeParser = new CSharpCodeParser(context);
            var markupParser = new HtmlMarkupParser(context);

            codeParser.HtmlParser = markupParser;
            markupParser.CodeParser = codeParser;

            // Execute the parse
            markupParser.ParseDocument();

            // Get the result
            var razorSyntaxTree = context.BuildRazorSyntaxTree();

            // Return the new result
            return razorSyntaxTree;
        }
    }
}
