// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class HtmlErrorTest : ParserTestBase
{
    [Fact]
    public void AllowsInvalidTagNamesAsLongAsParserCanIdentifyEndTag()
    {
        ParseDocumentTest("@{<1-foo+bar>foo</1-foo+bar>}");
    }

    [Fact]
    public void ErrorIfStartTextTagContainsTextAfterName()
    {
        ParseDocumentTest("@{<text foo bar></text>}");
    }

    [Fact]
    public void ErrorIfEndTextTagContainsTextAfterName()
    {
        ParseDocumentTest("@{<text></text foo bar>}");
    }

    [Fact]
    public void StartingWithEndTagErrorsThenOutputsMarkupSegmentAndEndsBlock()
    {
        ParseDocumentTest("@{</foo> bar baz}");
    }

    [Fact]
    public void WithUnclosedTopLevelTagErrorsOnOutermostUnclosedTag()
    {
        ParseDocumentTest("@{<p><foo></bar>}");
    }

    [Fact]
    public void WithUnclosedTagAtEOFErrorsOnMissingEndTag()
    {
        ParseDocumentTest("@{<foo>blah blah blah blah blah");
    }

    [Fact]
    public void WithUnfinishedTagAtEOFErrorsWithIncompleteTag()
    {
        ParseDocumentTest("@{<foo bar=baz");
    }
}
