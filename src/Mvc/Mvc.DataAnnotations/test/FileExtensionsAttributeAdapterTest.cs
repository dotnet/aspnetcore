// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Localization;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class FileExtensionsAttributeAdapterTest
{
    [Theory]
    [InlineData("", ".png,.jpg,.jpeg,.gif")]
    [InlineData(null, ".png,.jpg,.jpeg,.gif")]
    [ReplaceCulture]
    public void AddValidation_WithoutLocalizationAndDefaultFileExtensions(string extensions, string expectedExtensions)
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(Profile), nameof(Profile.PhotoFileName));

        var attribute = new FileExtensionsAttribute() { Extensions = extensions };
        attribute.ErrorMessage = "{0} expects only the following extensions: {1}";

        // FileExtensionsAttribute formats the extension list for the error message
        var formattedExtensions = string.Join(", ", expectedExtensions.Split(','));
        var expectedErrorMessage = string.Format(CultureInfo.CurrentCulture, attribute.ErrorMessage, nameof(Profile.PhotoFileName), formattedExtensions);

        var adapter = new FileExtensionsAttributeAdapter(attribute, stringLocalizer: null);
        var context = new ClientModelValidationContext(new ActionContext(), metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-fileextensions", kvp.Key); Assert.Equal(expectedErrorMessage, kvp.Value); },
            kvp => { Assert.Equal("data-val-fileextensions-extensions", kvp.Key); Assert.Equal(expectedExtensions, kvp.Value); });
    }

    public static TheoryData<string, string> ExtensionsData
    {
        get
        {
            return new TheoryData<string, string>()
                {
                    { "jpg", ".jpg" },
                    { " j p g ", ".jpg" },
                    { ".jpg", ".jpg" },
                    { ".x", ".x" },
                    { "jpg,png", ".jpg,.png" },
                    { "jpg, png", ".jpg,.png" },
                    { "JPG, Png", ".jpg,.png" },
                    { ".jpg,.png", ".jpg,.png" },
                    { "..jpg,..png", ".jpg,.png" },
                    { ".TXT, .png", ".txt,.png" },
                    { ".pdf , .docx", ".pdf,.docx" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ExtensionsData))]
    [ReplaceCulture]
    public void AddValidation_WithoutLocalizationAndCustomFileExtensions(string extensions, string expectedExtensions)
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(Profile), nameof(Profile.PhotoFileName));

        var attribute = new FileExtensionsAttribute() { Extensions = extensions };
        attribute.ErrorMessage = "{0} expects only the following extensions: {1}";

        // FileExtensionsAttribute formats the extension list for the error message
        var formattedExtensions = string.Join(", ", expectedExtensions.Split(','));
        var expectedErrorMessage = string.Format(CultureInfo.CurrentCulture, attribute.ErrorMessage, nameof(Profile.PhotoFileName), formattedExtensions);

        var adapter = new FileExtensionsAttributeAdapter(attribute, stringLocalizer: null);
        var context = new ClientModelValidationContext(new ActionContext(), metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-fileextensions", kvp.Key); Assert.Equal(expectedErrorMessage, kvp.Value); },
            kvp => { Assert.Equal("data-val-fileextensions-extensions", kvp.Key); Assert.Equal(expectedExtensions, kvp.Value); });
    }

    [Theory]
    [MemberData(nameof(ExtensionsData))]
    [ReplaceCulture]
    public void AddValidation_WithLocalization(string extensions, string expectedExtensions)
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(Profile), nameof(Profile.PhotoFileName));

        var attribute = new FileExtensionsAttribute() { Extensions = extensions };
        attribute.ErrorMessage = "{0} expects only the following extensions: {1}";

        var formattedExtensions = string.Join(", ", expectedExtensions.Split(','));
        var expectedProperties = new object[] { "PhotoFileName", formattedExtensions };
        var expectedErrorMessage = $"{nameof(Profile.PhotoFileName)} expects only the following extensions: {formattedExtensions}";

        var stringLocalizer = new Mock<IStringLocalizer>();
        stringLocalizer
            .Setup(s => s[attribute.ErrorMessage, expectedProperties])
            .Returns(new LocalizedString(attribute.ErrorMessage, expectedErrorMessage));

        var adapter = new FileExtensionsAttributeAdapter(attribute, stringLocalizer: stringLocalizer.Object);
        var context = new ClientModelValidationContext(new ActionContext(), metadata, provider, new Dictionary<string, string>());

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-fileextensions", kvp.Key); Assert.Equal(expectedErrorMessage, kvp.Value); },
            kvp => { Assert.Equal("data-val-fileextensions-extensions", kvp.Key); Assert.Equal(expectedExtensions, kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void AddValidation_DoesNotTrounceExistingAttributes()
    {
        // Arrange
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var metadata = provider.GetMetadataForProperty(typeof(Profile), "PhotoFileName");

        var attribute = new FileExtensionsAttribute() { Extensions = "jpg" };
        attribute.ErrorMessage = "{0} expects only the following extensions: {1}";

        var adapter = new FileExtensionsAttributeAdapter(attribute, stringLocalizer: null);
        var context = new ClientModelValidationContext(new ActionContext(), metadata, provider, new Dictionary<string, string>());

        context.Attributes.Add("data-val", "original");
        context.Attributes.Add("data-val-fileextensions", "original");
        context.Attributes.Add("data-val-fileextensions-extensions", "original");

        // Act
        adapter.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("original", kvp.Value); },
            kvp => { Assert.Equal("data-val-fileextensions", kvp.Key); Assert.Equal("original", kvp.Value); },
            kvp => { Assert.Equal("data-val-fileextensions-extensions", kvp.Key); Assert.Equal("original", kvp.Value); });
    }

    private class Profile
    {
        public string PhotoFileName { get; set; }
    }
}
