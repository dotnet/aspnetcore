// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Razor.Test.Framework
{
    public abstract class VBHtmlCodeParserTestBase : CodeParserTestBase
    {
        protected override ISet<string> KeywordSet
        {
            get { return VBCodeParser.DefaultKeywords; }
        }

        protected override SpanFactory CreateSpanFactory()
        {
            return SpanFactory.CreateVbHtml();
        }

        public override ParserBase CreateMarkupParser()
        {
            return new HtmlMarkupParser();
        }

        public override ParserBase CreateCodeParser()
        {
            return new VBCodeParser();
        }
    }
}
