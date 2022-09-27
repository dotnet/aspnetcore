// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

public class DisplayMetadataTest
{
    [Fact]
    public void DisplayFormatString_AffectsBothDisplayFormatProperties()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act
        displayMetadata.DisplayFormatString = "expected string";

        // Assert
        Assert.Equal("expected string", displayMetadata.DisplayFormatString);
        Assert.Equal("expected string", displayMetadata.DisplayFormatStringProvider());
    }

    [Fact]
    public void DisplayFormatStringProvider_AffectsBothDisplayFormatProperties()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act
        displayMetadata.DisplayFormatStringProvider = () => "expected string";

        // Assert
        Assert.Equal("expected string", displayMetadata.DisplayFormatString);
        Assert.Equal("expected string", displayMetadata.DisplayFormatStringProvider());
    }

    [Fact]
    public void DisplayFormatString_LastSettingWins()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act 1
        displayMetadata.DisplayFormatString = "first string";

        // Assert 1
        Assert.Equal("first string", displayMetadata.DisplayFormatString);
        Assert.Equal("first string", displayMetadata.DisplayFormatStringProvider());

        // Act 2
        displayMetadata.DisplayFormatStringProvider = () => "second string";

        // Assert 2
        Assert.Equal("second string", displayMetadata.DisplayFormatString);
        Assert.Equal("second string", displayMetadata.DisplayFormatStringProvider());

        // Act 3
        displayMetadata.DisplayFormatString = "third string";

        // Assert 3
        Assert.Equal("third string", displayMetadata.DisplayFormatString);
        Assert.Equal("third string", displayMetadata.DisplayFormatStringProvider());
    }

    [Fact]
    public void EditFormatString_AffectsBothEditFormatProperties()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act
        displayMetadata.EditFormatString = "expected string";

        // Assert
        Assert.Equal("expected string", displayMetadata.EditFormatString);
        Assert.Equal("expected string", displayMetadata.EditFormatStringProvider());
    }

    [Fact]
    public void EditFormatStringProvider_AffectsBothEditFormatProperties()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act
        displayMetadata.EditFormatStringProvider = () => "expected string";

        // Assert
        Assert.Equal("expected string", displayMetadata.EditFormatString);
        Assert.Equal("expected string", displayMetadata.EditFormatStringProvider());
    }

    [Fact]
    public void EditFormatString_LastSettingWins()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act 1
        displayMetadata.EditFormatString = "first string";

        // Assert 1
        Assert.Equal("first string", displayMetadata.EditFormatString);
        Assert.Equal("first string", displayMetadata.EditFormatStringProvider());

        // Act 2
        displayMetadata.EditFormatStringProvider = () => "second string";

        // Assert 2
        Assert.Equal("second string", displayMetadata.EditFormatString);
        Assert.Equal("second string", displayMetadata.EditFormatStringProvider());

        // Act 3
        displayMetadata.EditFormatString = "third string";

        // Assert 3
        Assert.Equal("third string", displayMetadata.EditFormatString);
        Assert.Equal("third string", displayMetadata.EditFormatStringProvider());
    }

    [Fact]
    public void NullDisplayText_AffectsBothNullDisplayProperties()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act
        displayMetadata.NullDisplayText = "expected string";

        // Assert
        Assert.Equal("expected string", displayMetadata.NullDisplayText);
        Assert.Equal("expected string", displayMetadata.NullDisplayTextProvider());
    }

    [Fact]
    public void NullDisplayTextProvider_AffectsBothNullDisplayProperties()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act
        displayMetadata.NullDisplayTextProvider = () => "expected string";

        // Assert
        Assert.Equal("expected string", displayMetadata.NullDisplayText);
        Assert.Equal("expected string", displayMetadata.NullDisplayTextProvider());
    }

    [Fact]
    public void NullDisplayText_LastSettingWins()
    {
        // Arrange
        var displayMetadata = new DisplayMetadata();

        // Act 1
        displayMetadata.NullDisplayText = "first string";

        // Assert 1
        Assert.Equal("first string", displayMetadata.NullDisplayText);
        Assert.Equal("first string", displayMetadata.NullDisplayTextProvider());

        // Act 2
        displayMetadata.NullDisplayTextProvider = () => "second string";

        // Assert 2
        Assert.Equal("second string", displayMetadata.NullDisplayText);
        Assert.Equal("second string", displayMetadata.NullDisplayTextProvider());

        // Act 3
        displayMetadata.NullDisplayText = "third string";

        // Assert 3
        Assert.Equal("third string", displayMetadata.NullDisplayText);
        Assert.Equal("third string", displayMetadata.NullDisplayTextProvider());
    }
}
