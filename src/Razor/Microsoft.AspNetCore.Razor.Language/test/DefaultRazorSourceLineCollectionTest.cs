// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test;

public class DefaultRazorSourceLineCollectionTest
{
    [Fact]
    public void GetLocation_Negative()
    {
        // Arrange
        var content = @"@addTagHelper, * Stuff
@* A comment *@";
        var document = TestRazorSourceDocument.Create(content);
        var collection = new DefaultRazorSourceLineCollection(document);

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => collection.GetLocation(-1));
    }

    [Fact]
    public void GetLocation_TooBig()
    {
        // Arrange
        var content = @"addTagHelper, * Stuff
@* A comment *@";
        var document = TestRazorSourceDocument.Create(content);
        var collection = new DefaultRazorSourceLineCollection(document);

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => collection.GetLocation(40));
    }

    [Fact]
    public void GetLocation_AtStart()
    {
        // Arrange
        var content = @"@addTaghelper, * Stuff
@* A comment *@";
        var document = TestRazorSourceDocument.Create(content);
        var collection = new DefaultRazorSourceLineCollection(document);

        // Act
        var location = collection.GetLocation(0);

        // Assert
        var expected = new SourceLocation("test.cshtml", 0, 0, 0);
        Assert.Equal(expected, location);
    }

    [Fact]
    public void GetLocation_AtEnd()
    {
        // Arrange
        var content = @"@addTagHelper, * Stuff
@* A comment *@";
        var document = TestRazorSourceDocument.Create(content);
        var collection = new DefaultRazorSourceLineCollection(document);
        var length = content.Length;

        // Act
        var location = collection.GetLocation(length);

        // Assert
        var expected = new SourceLocation("test.cshtml", length, 1, 15);
        Assert.Equal(expected, location);
    }

    [Fact]
    public void GetLineLength_Negative()
    {
        // Arrange
        var content = @"@addTagHelper, * Stuff
@* A comment *@";
        var document = TestRazorSourceDocument.Create(content);
        var collection = new DefaultRazorSourceLineCollection(document);

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => collection.GetLineLength(-1));
    }

    [Fact]
    public void GetLineLength_Bigger()
    {
        // Arrange
        var content = @"@addTagHelper, * Stuff
@* A comment *@";
        var document = TestRazorSourceDocument.Create(content);
        var collection = new DefaultRazorSourceLineCollection(document);

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => collection.GetLineLength(40));
    }

    [Fact]
    public void GetLineLength_AtStart()
    {
        // Arrange
        var content = @"@addTagHelper, * Stuff
@* A comment *@";
        var document = TestRazorSourceDocument.Create(content);
        var collection = new DefaultRazorSourceLineCollection(document);

        // Act
        var lineLength = collection.GetLineLength(0);

        // Assert
        var expectedLineLength = 22 + Environment.NewLine.Length;
        Assert.Equal(expectedLineLength, lineLength);
    }

    [Fact]
    public void GetLineLength_AtEnd()
    {
        // Arrange
        var content = @"@addTagHelper, * Stuff
@* A comment *@";
        var document = TestRazorSourceDocument.Create(content);
        var collection = new DefaultRazorSourceLineCollection(document);

        // Act
        var lineLength = collection.GetLineLength(1);

        // Assert
        var expectedLineLength = 15;
        Assert.Equal(expectedLineLength, lineLength);
    }
}
