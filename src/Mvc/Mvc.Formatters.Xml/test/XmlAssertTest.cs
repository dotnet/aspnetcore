// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class XmlAssertTest
{
    [Theory]
    [InlineData("<A>hello</A>", "<A>hey</A>")]
    [InlineData("<A><B>hello world</B></A>", "<A><B>hello world!!</B></A>")]
    [InlineData("<a><b>hello</b><b>world</b></a>", "<a><b>hello</b><c>world</c></a>")]
    [InlineData("<a><b>hello</b><b>world</b></a>", "<a><b>hello</b><b attribute=\"value\">world</b></a>")]
    [InlineData("<a>hello<b>world</b>hello</a>", "<a>hello<b>world</b>goodbye</a>")]
    [InlineData("<a><b>hello</b><b>world</b></a>", "<a><b>hello</b><b>goodbye</b></a>")]
    public void Throws_WithMismatchedTextNodes(string input1, string input2)
    {
        var equalException = Assert.Throws<EqualException>(() => XmlAssert.Equal(input1, input2));
        Assert.Contains(input1, equalException.Message);
        Assert.Contains(input2, equalException.Message);
    }

    [Theory]
    [InlineData("<a>hello<b>world</b>hello</a>", "<a>hello<b>world</b>hello</a>")]
    [InlineData("<a>hello<b/>hello</a>", "<a>hello<b/>hello</a>")]
    [InlineData(
        "<a>hello<b color=\"red\" siz=\"medium\">world</b>hello</a>",
        "<a>hello<b siz=\"medium\" color=\"red\">world</b>hello</a>")]
    public void ReturnsSuccessfully_WithMatchingTextNodes(string input1, string input2)
    {
        XmlAssert.Equal(input1, input2);
    }

    public static TheoryData<string, string> EmptyElementData
    {
        get
        {
            var data = new TheoryData<string, string>
                {
                    { "<A></A>", "<A></A>" },
                    { "<A><!-- comment1 --><B></B></A>", "<A><!-- comment1 --><B></B></A>" },
                    { "<A/>", "<A/>" },
                };

            // DeepEquals returns false even though the generated XML documents are equal.
            // This is fixed in Mono 4.3.0
            if (!TestPlatformHelper.IsMono)
            {
                data.Add("<A><![CDATA[<greeting></greeting>]]></A>", "<A><![CDATA[<greeting></greeting>]]></A>");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EmptyElementData))]
    public void ReturnsSuccessfully_WithEmptyElements(string input1, string input2)
    {
        XmlAssert.Equal(input1, input2);
    }

    [Theory]
    [InlineData("<?xml version=\"1.0\" encoding=\"UTF-8\"?><A></A>",
        "<A></A>")]
    [InlineData("<?xml version=\"1.0\" encoding=\"UTF-8\"?><A></A>",
        "<?xml version=\"1.0\" encoding=\"UTF-16\"?><A></A>")]
    public void Throws_WithMismatchedXmlDeclaration(string input1, string input2)
    {
        Assert.Throws<EqualException>(() => XmlAssert.Equal(input1, input2));
    }

    [ConditionalFact]
    // DeepEquals returns false even though the generated XML documents are equal.
    // This is fixed in Mono 4.3.0
    [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
    public void ReturnsSuccessfully_WithMatchingXmlDeclaration_IgnoringCase()
    {
        // Arrange
        var input1 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<A><B color=\"red\" size=\"medium\">hello world</B></A>";
        var input2 = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<A><B size=\"medium\" color=\"red\">hello world</B></A>";

        // Act and Assert
        XmlAssert.Equal(input1, input2);
    }

    [Theory]
    [InlineData(
        "<A color=\"red\" size=\"medium\" />",
        "<A size=\"medium\" color=\"red\" />")]
    [InlineData(
        "<A><B color=\"red\" size=\"medium\">hello world</B></A>",
        "<A><B size=\"medium\" color=\"red\">hello world</B></A>")]
    public void ReturnsSuccessfully_WithMatchingContent_IgnoringAttributeOrder(string input1, string input2)
    {
        XmlAssert.Equal(input1, input2);
    }

    [Fact]
    public void Throws_WithMismatchedAttributeValues_ReorderingAttributes()
    {
        // Arrange
        var expected = "<A>hello<B color=\"red\" size=\"medium\">hello world</B>hi</A>";
        var actual = "<A>hello<B size=\"Medium\" color=\"red\">hello world</B>hi</A>";
        var exceptionMessageForExpected = "<A>hello<B color=\"red\" size=\"medium\">hello world</B>hi</A>";
        var exceptionMessageForActual = "<A>hello<B color=\"red\" size=\"Medium\">hello world</B>hi</A>";

        // Act and Assert
        var equalException = Assert.Throws<EqualException>(() => XmlAssert.Equal(expected, actual));
        Assert.Contains(exceptionMessageForExpected, equalException.Message);
        Assert.Contains(exceptionMessageForActual, equalException.Message);
    }
}
