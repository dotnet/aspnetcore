// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class RepalceCultureAttributeTest
{
    [Fact]
    public void DefaultsTo_EnGB_EnUS()
    {
        // Arrange
        var culture = new CultureInfo("en-GB");
        var uiCulture = new CultureInfo("en-US");

        // Act
        var replaceCulture = new ReplaceCultureAttribute();

        // Assert
        Assert.Equal(culture, replaceCulture.Culture);
        Assert.Equal(uiCulture, replaceCulture.UICulture);
    }

    [Fact]
    public void UsesSuppliedCultureAndUICulture()
    {
        // Arrange
        var culture = "de-DE";
        var uiCulture = "fr-CA";

        // Act
        var replaceCulture = new ReplaceCultureAttribute(culture, uiCulture);

        // Assert
        Assert.Equal(new CultureInfo(culture), replaceCulture.Culture);
        Assert.Equal(new CultureInfo(uiCulture), replaceCulture.UICulture);
    }

    [Fact]
    public void BeforeAndAfterTest_ReplacesCulture()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        var culture = "de-DE";
        var uiCulture = "fr-CA";
        var replaceCulture = new ReplaceCultureAttribute(culture, uiCulture);

        // Act
        replaceCulture.Before(methodUnderTest: null);

        // Assert
        Assert.Equal(new CultureInfo(culture), CultureInfo.CurrentCulture);
        Assert.Equal(new CultureInfo(uiCulture), CultureInfo.CurrentUICulture);

        // Act
        replaceCulture.After(methodUnderTest: null);

        // Assert
        Assert.Equal(originalCulture, CultureInfo.CurrentCulture);
        Assert.Equal(originalUICulture, CultureInfo.CurrentUICulture);
    }
}
