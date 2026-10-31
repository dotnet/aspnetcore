// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Localization;

public class RequestLocalizationOptionsTest : IDisposable
{
    private readonly CultureInfo _initialCulture;
    private readonly CultureInfo _initialUICulture;

    public RequestLocalizationOptionsTest()
    {
        _initialCulture = CultureInfo.CurrentCulture;
        _initialUICulture = CultureInfo.CurrentUICulture;
    }

    [Fact]
    public void DefaultRequestCulture_DefaultsToCurrentCulture()
    {
        // Arrange/Act
        var options = new RequestLocalizationOptions();

        // Assert
        Assert.NotNull(options.DefaultRequestCulture);
        Assert.Equal(CultureInfo.CurrentCulture, options.DefaultRequestCulture.Culture);
        Assert.Equal(CultureInfo.CurrentUICulture, options.DefaultRequestCulture.UICulture);
    }

    [Fact]
    public void DefaultRequestCulture_DefaultsToCurrentCultureWhenExplicitlySet()
    {
        // Arrange
        var explicitCulture = new CultureInfo("fr-FR");
        CultureInfo.CurrentCulture = explicitCulture;
        CultureInfo.CurrentUICulture = explicitCulture;

        // Act
        var options = new RequestLocalizationOptions();

        // Assert
        Assert.Equal(explicitCulture, options.DefaultRequestCulture.Culture);
        Assert.Equal(explicitCulture, options.DefaultRequestCulture.UICulture);
    }

    [Fact]
    public void DefaultRequestCulture_ThrowsWhenTryingToSetToNull()
    {
        // Arrange
        var options = new RequestLocalizationOptions();

        // Act/Assert
        Assert.Throws<ArgumentNullException>(() => options.DefaultRequestCulture = null);
    }

    [Fact]
    public void SupportedCultures_DefaultsToCurrentCulture()
    {
        // Arrange/Act
        var options = new RequestLocalizationOptions();

        // Assert
        Assert.Collection(options.SupportedCultures, item => Assert.Equal(CultureInfo.CurrentCulture, item));
        Assert.Collection(options.SupportedUICultures, item => Assert.Equal(CultureInfo.CurrentUICulture, item));
    }

    [Fact]
    public void SupportedCultures_DefaultsToCurrentCultureWhenExplicitlySet()
    {
        // Arrange
        var explicitCulture = new CultureInfo("fr-FR");
        CultureInfo.CurrentCulture = explicitCulture;
        CultureInfo.CurrentUICulture = explicitCulture;

        // Act
        var options = new RequestLocalizationOptions();

        // Assert
        Assert.Collection(options.SupportedCultures, item => Assert.Equal(explicitCulture, item));
        Assert.Collection(options.SupportedUICultures, item => Assert.Equal(explicitCulture, item));
    }

    [Fact]
    public void BuilderAPIs_AddSupportedCultures()
    {
        // Arrange
        var supportedCultures = new[] { "en-US", "ar-YE" };

        // Act
        var options = new RequestLocalizationOptions()
            .AddSupportedCultures(supportedCultures);

        // Assert
        Assert.Equal(supportedCultures, options.SupportedCultures.Select(c => c.Name));
    }

    [Fact]
    public void BuilderAPIs_AddSupportedUICultures()
    {
        // Arrange
        var supportedUICultures = new[] { "en-US", "ar-YE" };

        // Act
        var options = new RequestLocalizationOptions()
            .AddSupportedUICultures(supportedUICultures);

        // Assert
        Assert.Equal(supportedUICultures, options.SupportedUICultures.Select(c => c.Name));
    }

    [Fact]
    public void BuilderAPIs_SetDefaultCulture()
    {
        // Arrange
        var defaultCulture = "ar-YE";

        // Act
        var options = new RequestLocalizationOptions()
            .SetDefaultCulture(defaultCulture);

        // Assert
        Assert.Equal(defaultCulture, options.DefaultRequestCulture.Culture.Name);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _initialCulture;
        CultureInfo.CurrentUICulture = _initialUICulture;
    }
}
