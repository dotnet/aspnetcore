// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using Resources = Microsoft.AspNetCore.Mvc.ViewFeatures.Test.Resources;

namespace Microsoft.AspNetCore.Mvc;

public class RemoteAttributeBaseTest
{
    [Fact]
    public void IsValidAlwaysReturnsTrue()
    {
        // Arrange
        var attribute = new TestableRemoteAttributeBase();

        // Act & Assert
        Assert.True(attribute.IsValid(value: null));
    }

    [Fact]
    public void ErrorMessageProperties_HaveExpectedDefaultValues()
    {
        // Arrange & Act
        var attribute = new TestableRemoteAttributeBase();

        // Assert
        Assert.Null(attribute.ErrorMessage);
        Assert.Null(attribute.ErrorMessageResourceName);
        Assert.Null(attribute.ErrorMessageResourceType);
    }

    [Fact]
    [ReplaceCulture]
    public void FormatErrorMessage_ReturnsDefaultErrorMessage()
    {
        // Arrange
        // See ViewFeatures.Resources.RemoteAttribute_RemoteValidationFailed.
        var expected = "'Property1' is invalid.";
        var attribute = new TestableRemoteAttributeBase();

        // Act
        var message = attribute.FormatErrorMessage("Property1");

        // Assert
        Assert.Equal(expected, message);
    }

    [Fact]
    public void FormatErrorMessage_UsesOverriddenErrorMessage()
    {
        // Arrange
        var expected = "Error about 'Property1' from override.";
        var attribute = new TestableRemoteAttributeBase()
        {
            ErrorMessage = "Error about '{0}' from override.",
        };

        // Act
        var message = attribute.FormatErrorMessage("Property1");

        // Assert
        Assert.Equal(expected, message);
    }

    [Fact]
    [ReplaceCulture]
    public void FormatErrorMessage_UsesErrorMessageFromResource()
    {
        // Arrange
        var expected = "Error about 'Property1' from resources.";
        var attribute = new TestableRemoteAttributeBase()
        {
            ErrorMessageResourceName = nameof(Resources.RemoteAttribute_Error),
            ErrorMessageResourceType = typeof(Resources)
        };

        // Act
        var message = attribute.FormatErrorMessage("Property1");

        // Assert
        Assert.Equal(expected, message);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void FormatAdditionalFieldsForClientValidation_WithInvalidPropertyName_Throws(string property, string expectedMessage)
    {
        // Arrange
        var attribute = new TestableRemoteAttributeBase();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => attribute.FormatAdditionalFieldsForClientValidation(property),
            "property",
            expectedMessage);
    }

    [Fact]
    public void FormatAdditionalFieldsForClientValidation_WillFormat_AdditionalFields()
    {
        // Arrange
        var attribute = new TestableRemoteAttributeBase
        {
            AdditionalFields = "FieldOne, FieldTwo"
        };

        // Act
        var actual = attribute.FormatAdditionalFieldsForClientValidation("Property");

        // Assert
        var expected = "*.Property,*.FieldOne,*.FieldTwo";
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void FormatPropertyForClientValidation_WithInvalidPropertyName_Throws(string property, string expectedMessage)
    {
        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => RemoteAttributeBase.FormatPropertyForClientValidation(property),
            "property",
            expectedMessage);
    }

    [Fact]
    public void AddValidation_WithErrorMessage_SetsAttributesAsExpected()
    {
        // Arrange
        var expected = "Error about 'Length' from override.";
        var url = "/Controller/Action";
        var context = GetValidationContext();
        var attribute = new TestableRemoteAttributeBase(dummyGetUrlReturnValue: url)
        {
            HttpMethod = "POST",
            ErrorMessage = "Error about '{0}' from override.",
        };

        // Act
        attribute.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp =>
            {
                Assert.Equal("data-val-remote", kvp.Key);
                Assert.Equal(expected, kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                Assert.Equal("*.Length", kvp.Value);
            },
            kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });
    }

    [Fact]
    public void AddValidation_WithErrorMessageAndLocalizerFactory_SetsAttributesAsExpected()
    {
        // Arrange
        var expected = "Error about 'Length' from override.";
        var url = "/Controller/Action";
        var localizerFactory = new Mock<IStringLocalizerFactory>(MockBehavior.Strict).Object;
        var context = GetValidationContext(localizerFactory);
        var attribute = new TestableRemoteAttributeBase(dummyGetUrlReturnValue: url)
        {
            HttpMethod = "POST",
            ErrorMessage = "Error about '{0}' from override.",
        };

        // Act
        attribute.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp =>
            {
                // IStringLocalizerFactory existence alone is insufficient to change error message.
                Assert.Equal("data-val-remote", kvp.Key);
                Assert.Equal(expected, kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                Assert.Equal("*.Length", kvp.Value);
            },
            kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });
    }

    [Fact]
    public void AddValidation_WithErrorMessageAndLocalizerProvider_SetsAttributesAsExpected()
    {
        // Arrange
        var expected = "Error about 'Length' from override.";
        var url = "/Controller/Action";
        var context = GetValidationContext();
        var attribute = new TestableRemoteAttributeBase(url)
        {
            HttpMethod = "POST",
            ErrorMessage = "Error about '{0}' from override.",
        };

        var options = context.ActionContext.HttpContext.RequestServices
            .GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();
        var localizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
        options.Value.DataAnnotationLocalizerProvider = (type, factory) => localizer.Object;

        // Act
        attribute.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp =>
            {
                // Non-null DataAnnotationLocalizerProvider alone is insufficient to change error message.
                Assert.Equal("data-val-remote", kvp.Key);
                Assert.Equal(expected, kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                Assert.Equal("*.Length", kvp.Value);
            },
            kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });
    }

    [Fact]
    public void AddValidation_WithErrorMessageLocalizerFactoryAndLocalizerProvider_SetsAttributesAsExpected()
    {
        // Arrange
        var expected = "Error about 'Length' from localizer.";
        var url = "/Controller/Action";
        var localizerFactory = new Mock<IStringLocalizerFactory>(MockBehavior.Strict).Object;
        var context = GetValidationContext(localizerFactory);
        var attribute = new TestableRemoteAttributeBase(dummyGetUrlReturnValue: url)
        {
            HttpMethod = "POST",
            ErrorMessage = "Error about '{0}' from override.",
        };

        var localizedString = new LocalizedString("Fred", expected);
        var localizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
        localizer
            .Setup(l => l["Error about '{0}' from override.", "Length"])
            .Returns(localizedString)
            .Verifiable();
        var options = context.ActionContext.HttpContext.RequestServices
            .GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();
        options.Value.DataAnnotationLocalizerProvider = (type, factory) => localizer.Object;

        // Act
        attribute.AddValidation(context);

        // Assert
        localizer.VerifyAll();

        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp =>
            {
                Assert.Equal("data-val-remote", kvp.Key);
                Assert.Equal(expected, kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                Assert.Equal("*.Length", kvp.Value);
            },
            kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });
    }

    [Fact]
    [ReplaceCulture]
    public void AddValidation_WithErrorResourcesLocalizerFactoryAndLocalizerProvider_SetsAttributesAsExpected()
    {
        // Arrange
        var expected = "Error about 'Length' from resources.";
        var url = "/Controller/Action";
        var attribute = new TestableRemoteAttributeBase(dummyGetUrlReturnValue: url)
        {
            HttpMethod = "POST",
            ErrorMessageResourceName = nameof(Resources.RemoteAttribute_Error),
            ErrorMessageResourceType = typeof(Resources),
        };

        var localizerFactory = new Mock<IStringLocalizerFactory>(MockBehavior.Strict).Object;
        var context = GetValidationContext(localizerFactory);

        var localizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
        var options = context.ActionContext.HttpContext.RequestServices
            .GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();
        options.Value.DataAnnotationLocalizerProvider = (type, factory) => localizer.Object;

        // Act
        attribute.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp =>
            {
                // Configuring the attribute using ErrorMessageResource* trumps available IStringLocalizer etc.
                Assert.Equal("data-val-remote", kvp.Key);
                Assert.Equal(expected, kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                Assert.Equal("*.Length", kvp.Value);
            },
            kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });
    }

    [Fact]
    public void AddValidation_WithErrorMessageAndDisplayName_SetsAttributesAsExpected()
    {
        // Arrange
        var expected = "Error about 'Display Length' from override.";
        var url = "/Controller/Action";

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty(typeof(string), nameof(string.Length))
            .DisplayDetails(d => d.DisplayName = () => "Display Length");
        var context = GetValidationContext(localizerFactory: null, metadataProvider: metadataProvider);

        var attribute = new TestableRemoteAttributeBase(dummyGetUrlReturnValue: url)
        {
            HttpMethod = "POST",
            ErrorMessage = "Error about '{0}' from override.",
        };

        // Act
        attribute.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp =>
            {
                Assert.Equal("data-val-remote", kvp.Key);
                Assert.Equal(expected, kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                Assert.Equal("*.Length", kvp.Value);
            },
            kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });
    }

    [Fact]
    public void AddValidation_WithErrorMessageLocalizerFactoryLocalizerProviderAndDisplayName_SetsAttributesAsExpected()
    {
        // Arrange
        var expected = "Error about 'Length' from localizer.";
        var url = "/Controller/Action";

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty(typeof(string), nameof(string.Length))
            .DisplayDetails(d => d.DisplayName = () => "Display Length");
        var localizerFactory = new Mock<IStringLocalizerFactory>(MockBehavior.Strict).Object;
        var context = GetValidationContext(localizerFactory, metadataProvider);

        var attribute = new TestableRemoteAttributeBase(dummyGetUrlReturnValue: url)
        {
            HttpMethod = "POST",
            ErrorMessage = "Error about '{0}' from override.",
        };

        var localizedString = new LocalizedString("Fred", expected);
        var localizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
        localizer
            .Setup(l => l["Error about '{0}' from override.", "Display Length"])
            .Returns(localizedString)
            .Verifiable();
        var options = context.ActionContext.HttpContext.RequestServices
            .GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();
        options.Value.DataAnnotationLocalizerProvider = (type, factory) => localizer.Object;

        // Act
        attribute.AddValidation(context);

        // Assert
        localizer.VerifyAll();

        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp =>
            {
                Assert.Equal("data-val-remote", kvp.Key);
                Assert.Equal(expected, kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                Assert.Equal("*.Length", kvp.Value);
            },
            kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });
    }

    [Fact]
    public void AddValidation_WillSetAttributes_ToExpectedValues()
    {
        // Arrange
        var url = "/Controller/Action";
        var attribute = new TestableRemoteAttributeBase(dummyGetUrlReturnValue: url)
        {
            HttpMethod = "POST",
            AdditionalFields = "Password,ConfirmPassword",
            ErrorMessage = "Error"
        };
        var context = GetValidationContext();

        // Act
        attribute.AddValidation(context);

        // Assert
        Assert.Collection(
            context.Attributes,
            kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("Error", kvp.Value); },
            kvp =>
            {
                Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                Assert.Equal("*.Length,*.Password,*.ConfirmPassword", kvp.Value);
            },
            kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
            kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });
    }

    private static ClientModelValidationContext GetValidationContext(
        IStringLocalizerFactory localizerFactory = null,
        IModelMetadataProvider metadataProvider = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddOptions();
        if (localizerFactory != null)
        {
            serviceCollection.AddSingleton<IStringLocalizerFactory>(localizerFactory);
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };

        var actionContext = new ActionContext(
            httpContext,
            routeData: new Mock<RouteData>().Object,
            actionDescriptor: new ActionDescriptor());

        var emptyMetadataProvider = new EmptyModelMetadataProvider();

        if (metadataProvider == null)
        {
            metadataProvider = new EmptyModelMetadataProvider();
        }

        var metadata = metadataProvider.GetMetadataForProperty(
            containerType: typeof(string),
            propertyName: nameof(string.Length));

        return new ClientModelValidationContext(
            actionContext,
            metadata,
            metadataProvider,
            new AttributeDictionary());
    }

    private class TestableRemoteAttributeBase : RemoteAttributeBase
    {
        private readonly string _dummyGetUrlReturnValue;

        public TestableRemoteAttributeBase()
        { }

        public TestableRemoteAttributeBase(string dummyGetUrlReturnValue)
        {
            _dummyGetUrlReturnValue = dummyGetUrlReturnValue;
        }

        protected override string GetUrl(ClientModelValidationContext context)
        {
            return _dummyGetUrlReturnValue;
        }
    }
}
