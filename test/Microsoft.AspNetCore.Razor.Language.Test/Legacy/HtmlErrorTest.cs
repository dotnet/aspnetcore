// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlErrorTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void AllowsInvalidTagNamesAsLongAsParserCanIdentifyEndTag()
        {
            ParseBlockTest("<1-foo+bar>foo</1-foo+bar>");
        }

        [Fact]
        public void ThrowsErrorIfStartTextTagContainsTextAfterName()
        {
            ParseBlockTest("<text foo bar></text>");
        }

        [Fact]
        public void ThrowsErrorIfEndTextTagContainsTextAfterName()
        {
            ParseBlockTest("<text></text foo bar>");
        }

        [Fact]
        public void ThrowsExceptionIfBlockDoesNotStartWithTag()
        {
            ParseBlockTest("foo bar <baz>");
        }

        [Fact]
        public void StartingWithEndTagErrorsThenOutputsMarkupSegmentAndEndsBlock()
        {
            // ParseBlockStartingWithEndTagProducesRazorErrorThenOutputsMarkupSegmentAndEndsBlock
            ParseBlockTest("</foo> bar baz");
        }

        [Fact]
        public void WithUnclosedTopLevelTagThrowsOnOutermostUnclosedTag()
        {
            // ParseBlockWithUnclosedTopLevelTagThrowsMissingEndTagParserExceptionOnOutermostUnclosedTag
            ParseBlockTest("<p><foo></bar>");
        }

        [Fact]
        public void WithUnclosedTagAtEOFThrowsMissingEndTagException()
        {
            ParseBlockTest("<foo>blah blah blah blah blah");
        }

        [Fact]
        public void WithUnfinishedTagAtEOFThrowsIncompleteTagException()
        {
            ParseBlockTest("<foo bar=baz");
        }
    }
}
