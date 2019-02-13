// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Resources = Microsoft.AspNetCore.Mvc.ViewFeatures.Test.Resources;

namespace Microsoft.AspNetCore.Mvc
{
    public class RemoteAttributeTest
    {
        private static readonly IModelMetadataProvider _metadataProvider = new EmptyModelMetadataProvider();
        private static readonly ModelMetadata _metadata = _metadataProvider.GetMetadataForProperty(
            typeof(string),
            nameof(string.Length));

        public static TheoryData<string> SomeNames
        {
            get
            {
                return new TheoryData<string>
                {
                    string.Empty,
                    "Action",
                    "In a controller",
                    "  slightly\t odd\t whitespace\t\r\n",
                };
            }
        }

        // Null or empty property names are invalid. (Those containing just whitespace are legal.)
        public static TheoryData<string> NullOrEmptyNames
        {
            get
            {
                return new TheoryData<string>
                {
                    null,
                    string.Empty,
                };
            }
        }

        [Fact]
        public void IsValidAlwaysReturnsTrue()
        {
            // Act & Assert
            Assert.True(new RemoteAttribute("RouteName", "ParameterName").IsValid(value: null));
            Assert.True(new RemoteAttribute("ActionName", "ControllerName", "ParameterName").IsValid(value: null));
        }

        [Fact]
        public void Constructor_WithNullAction_IgnoresArgument()
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute(action: null, controller: "AController");

            // Assert
            var keyValuePair = Assert.Single(attribute.RouteData);
            Assert.Equal("controller", keyValuePair.Key);
        }

        [Fact]
        public void Constructor_WithNullController_IgnoresArgument()
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute("AnAction", controller: null);

            // Assert
            var keyValuePair = Assert.Single(attribute.RouteData);
            Assert.Equal("action", keyValuePair.Key);
            Assert.Null(attribute.RouteName);
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(SomeNames))]
        public void Constructor_WithRouteName_UpdatesProperty(string routeName)
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute(routeName);

            // Assert
            Assert.Empty(attribute.RouteData);
            Assert.Equal(routeName, attribute.RouteName);
        }

        [Theory]
        [MemberData(nameof(SomeNames))]
        public void Constructor_WithActionController_UpdatesActionRouteData(string action)
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute(action, "AController");

            // Assert
            Assert.Equal(2, attribute.RouteData.Count);
            Assert.Contains("controller", attribute.RouteData.Keys);
            var resultName = Assert.Single(
                    attribute.RouteData,
                    keyValuePair => string.Equals(keyValuePair.Key, "action", StringComparison.Ordinal))
                .Value;
            Assert.Equal(action, resultName);
            Assert.Null(attribute.RouteName);
        }

        [Theory]
        [MemberData(nameof(SomeNames))]
        public void Constructor_WithActionController_UpdatesControllerRouteData(string controller)
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute("AnAction", controller);

            // Assert
            Assert.Equal(2, attribute.RouteData.Count);
            Assert.Contains("action", attribute.RouteData.Keys);
            var resultName = Assert.Single(
                    attribute.RouteData,
                    keyValuePair => string.Equals(keyValuePair.Key, "controller", StringComparison.Ordinal))
                .Value;
            Assert.Equal(controller, resultName);
            Assert.Null(attribute.RouteName);
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(SomeNames))]
        public void Constructor_WithActionControllerAreaName_UpdatesAreaRouteData(string areaName)
        {
            // Arrange & Act
            var attribute = new TestableRemoteAttribute("AnAction", "AController", areaName: areaName);

            // Assert
            Assert.Equal(3, attribute.RouteData.Count);
            Assert.Contains("action", attribute.RouteData.Keys);
            Assert.Contains("controller", attribute.RouteData.Keys);
            var resultName = Assert.Single(
                    attribute.RouteData,
                    keyValuePair => string.Equals(keyValuePair.Key, "area", StringComparison.Ordinal))
                .Value;
            Assert.Equal(areaName, resultName);
            Assert.Null(attribute.RouteName);
        }

        [Fact]
        public void ErrorMessageProperties_HaveExpectedDefaultValues()
        {
            // Arrange & Act
            var attribute = new RemoteAttribute("Action", "Controller");

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
            var attribute = new RemoteAttribute("Action", "Controller");

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
            var attribute = new RemoteAttribute("Action", "Controller")
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
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                ErrorMessageResourceName = nameof(Resources.RemoteAttribute_Error),
                ErrorMessageResourceType = typeof(Resources),
            };

            // Act
            var message = attribute.FormatErrorMessage("Property1");

            // Assert
            Assert.Equal(expected, message);
        }

        [Theory]
        [MemberData(nameof(NullOrEmptyNames))]
        public void FormatAdditionalFieldsForClientValidation_WithInvalidPropertyName_Throws(string property)
        {
            // Arrange
            var attribute = new RemoteAttribute(routeName: "default");
            var expectedMessage = "Value cannot be null or empty.";

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => attribute.FormatAdditionalFieldsForClientValidation(property),
                "property",
                expectedMessage);
        }

        [Theory]
        [MemberData(nameof(NullOrEmptyNames))]
        public void FormatPropertyForClientValidation_WithInvalidPropertyName_Throws(string property)
        {
            // Arrange
            var expected = "Value cannot be null or empty.";

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => RemoteAttribute.FormatPropertyForClientValidation(property),
                "property",
                expected);
        }

        [Fact]
        public void AddValidation_WithBadRouteName_Throws()
        {
            // Arrange
            var attribute = new RemoteAttribute("nonexistentRoute");
            var context = GetValidationContextWithArea(currentArea: null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => attribute.AddValidation(context));
            Assert.Equal("No URL for remote validation could be found.", exception.Message);
        }

        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithRoute_CallsUrlHelperWithExpectedValues()
        {
            // Arrange
            var routeName = "RouteName";
            var attribute = new RemoteAttribute(routeName);
            var url = "/my/URL";
            var urlHelper = new MockUrlHelper(url, routeName);
            var context = GetValidationContext(urlHelper);

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });

            var routeDictionary = Assert.IsType<RouteValueDictionary>(urlHelper.RouteValues);
            Assert.Empty(routeDictionary);
        }

        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithActionController_CallsUrlHelperWithExpectedValues()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller");
            var url = "/Controller/Action";
            var urlHelper = new MockUrlHelper(url, routeName: null);
            var context = GetValidationContext(urlHelper);

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });

            var routeDictionary = Assert.IsType<RouteValueDictionary>(urlHelper.RouteValues);
            Assert.Equal(2, routeDictionary.Count);
            Assert.Equal("Action", routeDictionary["action"] as string);
            Assert.Equal("Controller", routeDictionary["controller"] as string);
        }

        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithActionController_PropertiesSet_CallsUrlHelperWithExpectedValues()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                AdditionalFields = "Password,ConfirmPassword",
            };
            var url = "/Controller/Action";
            var urlHelper = new MockUrlHelper(url, routeName: null);
            var context = GetValidationContext(urlHelper);

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                    Assert.Equal("*.Length,*.Password,*.ConfirmPassword", kvp.Value);
                },
                kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });

            var routeDictionary = Assert.IsType<RouteValueDictionary>(urlHelper.RouteValues);
            Assert.Equal(2, routeDictionary.Count);
            Assert.Equal("Action", routeDictionary["action"] as string);
            Assert.Equal("Controller", routeDictionary["controller"] as string);
        }

        [Fact]
        public void AddValidation_WithErrorMessage_SetsAttributesAsExpected()
        {
            // Arrange
            var expected = "Error about 'Length' from override.";
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                ErrorMessage = "Error about '{0}' from override.",
            };
            var url = "/Controller/Action";
            var context = GetValidationContext(url);

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
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                ErrorMessage = "Error about '{0}' from override.",
            };
            var url = "/Controller/Action";
            var context = GetValidationContextWithLocalizerFactory(url);

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
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                ErrorMessage = "Error about '{0}' from override.",
            };
            var url = "/Controller/Action";
            var context = GetValidationContext(url);

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
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                ErrorMessage = "Error about '{0}' from override.",
            };
            var url = "/Controller/Action";
            var context = GetValidationContextWithLocalizerFactory(url);

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
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                ErrorMessageResourceName = nameof(Resources.RemoteAttribute_Error),
                ErrorMessageResourceType = typeof(Resources),
            };
            var url = "/Controller/Action";
            var context = GetValidationContextWithLocalizerFactory(url);

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
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                ErrorMessage = "Error about '{0}' from override.",
            };

            var url = "/Controller/Action";
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForProperty(typeof(string), nameof(string.Length))
                .DisplayDetails(d => d.DisplayName = () => "Display Length");
            var context = GetValidationContext(url, metadataProvider);

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
            var attribute = new RemoteAttribute("Action", "Controller")
            {
                HttpMethod = "POST",
                ErrorMessage = "Error about '{0}' from override.",
            };

            var url = "/Controller/Action";
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForProperty(typeof(string), nameof(string.Length))
                .DisplayDetails(d => d.DisplayName = () => "Display Length");
            var context = GetValidationContextWithLocalizerFactory(url, metadataProvider);

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
        [ReplaceCulture]
        public void AddValidation_WithActionControllerArea_CallsUrlHelperWithExpectedValues()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "Test")
            {
                HttpMethod = "POST",
            };
            var url = "/Test/Controller/Action";
            var urlHelper = new MockUrlHelper(url, routeName: null);
            var context = GetValidationContext(urlHelper);

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-additionalfields", kvp.Key);
                    Assert.Equal("*.Length", kvp.Value);
                },
                kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("POST", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal(url, kvp.Value); });

            var routeDictionary = Assert.IsType<RouteValueDictionary>(urlHelper.RouteValues);
            Assert.Equal(3, routeDictionary.Count);
            Assert.Equal("Action", routeDictionary["action"] as string);
            Assert.Equal("Controller", routeDictionary["controller"] as string);
            Assert.Equal("Test", routeDictionary["area"] as string);
        }

        // Root area is current in this case.
        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithActionController_FindsControllerInCurrentArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller");
            var context = GetValidationContextWithArea(currentArea: null);

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-url", kvp.Key);
                    Assert.Equal("/Controller/Action", kvp.Value);
                });
        }

        // Test area is current in this case.
        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithActionControllerInArea_FindsControllerInCurrentArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller");
            var context = GetValidationContextWithArea(currentArea: "Test");

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-url", kvp.Key);
                    Assert.Equal("/Test/Controller/Action", kvp.Value);
                });
        }

        // Explicit reference to the (current) root area.
        [Theory]
        [MemberData(nameof(NullOrEmptyNames))]
        [ReplaceCulture]
        public void AddValidation_WithActionControllerArea_FindsControllerInRootArea(string areaName)
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", areaName);
            var context = GetValidationContextWithArea(currentArea: null);

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-url", kvp.Key);
                    Assert.Equal("/Controller/Action", kvp.Value);
                });
        }

        // Test area is current in this case.
        [Theory]
        [MemberData(nameof(NullOrEmptyNames))]
        [ReplaceCulture]
        public void AddValidation_WithActionControllerAreaInArea_FindsControllerInRootArea(string areaName)
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", areaName);
            var context = GetValidationContextWithArea(currentArea: "Test");

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-url", kvp.Key);
                    Assert.Equal("/Controller/Action", kvp.Value);
                });
        }

        // Root area is current in this case.
        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithActionControllerArea_FindsControllerInNamedArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "Test");
            var context = GetValidationContextWithArea(currentArea: null);

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-url", kvp.Key);
                    Assert.Equal("/Test/Controller/Action", kvp.Value);
                });
        }

        // Explicit reference to the current (Test) area.
        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithActionControllerAreaInArea_FindsControllerInNamedArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "Test");
            var context = GetValidationContextWithArea(currentArea: "Test");

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-url", kvp.Key);
                    Assert.Equal("/Test/Controller/Action", kvp.Value);
                });
        }

        // Test area is current in this case.
        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithActionControllerAreaInArea_FindsControllerInDifferentArea()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "AnotherArea");
            var context = GetValidationContextWithArea(currentArea: "Test");

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("'Length' is invalid.", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("*.Length", kvp.Value); },
                kvp =>
                {
                    Assert.Equal("data-val-remote-url", kvp.Key);
                    Assert.Equal("/AnotherArea/Controller/Action", kvp.Value);
                });
        }

        // Test area is current in this case.
        [Fact]
        public void AddValidation_DoesNotTrounceExistingAttributes()
        {
            // Arrange
            var attribute = new RemoteAttribute("Action", "Controller", "AnotherArea")
            {
                HttpMethod = "PUT",
            };

            var context = GetValidationContextWithArea(currentArea: "Test");

            context.Attributes.Add("data-val", "original");
            context.Attributes.Add("data-val-remote", "original");
            context.Attributes.Add("data-val-remote-additionalfields", "original");
            context.Attributes.Add("data-val-remote-type", "original");
            context.Attributes.Add("data-val-remote-url", "original");

            // Act
            attribute.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("original", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote", kvp.Key); Assert.Equal("original", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-additionalfields", kvp.Key); Assert.Equal("original", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-type", kvp.Key); Assert.Equal("original", kvp.Value); },
                kvp => { Assert.Equal("data-val-remote-url", kvp.Key); Assert.Equal("original", kvp.Value); });
        }

        private static ClientModelValidationContext GetValidationContext(
            string url,
            IModelMetadataProvider metadataProvider = null)
        {
            var urlHelper = new MockUrlHelper(url, routeName: null);
            return GetValidationContext(urlHelper, localizerFactory: null, metadataProvider: metadataProvider);
        }

        private static ClientModelValidationContext GetValidationContextWithLocalizerFactory(
            string url,
            IModelMetadataProvider metadataProvider = null)
        {
            var urlHelper = new MockUrlHelper(url, routeName: null);
            var localizerFactory = new Mock<IStringLocalizerFactory>(MockBehavior.Strict);
            return GetValidationContext(urlHelper, localizerFactory.Object, metadataProvider);
        }

        private static ClientModelValidationContext GetValidationContext(
            IUrlHelper urlHelper,
            IStringLocalizerFactory localizerFactory = null,
            IModelMetadataProvider metadataProvider = null)
        {
            var serviceCollection = GetServiceCollection(localizerFactory);
            var factory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
            serviceCollection.AddSingleton<IUrlHelperFactory>(factory.Object);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var actionContext = GetActionContext(serviceProvider, routeData: null);

            factory
                .Setup(f => f.GetUrlHelper(actionContext))
                .Returns(urlHelper);

            var metadata = _metadata;
            if (metadataProvider == null)
            {
                metadataProvider = _metadataProvider;
            }
            else
            {
                metadata = metadataProvider.GetMetadataForProperty(typeof(string), nameof(string.Length));
            }

            return new ClientModelValidationContext(
                actionContext,
                metadata,
                metadataProvider,
                new AttributeDictionary());
        }

        private static ClientModelValidationContext GetValidationContextWithArea(string currentArea)
        {
            var serviceCollection = GetServiceCollection(localizerFactory: null);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var routeCollection = GetRouteCollectionWithArea(serviceProvider);
            var routeData = new RouteData
            {
                Routers =
                {
                    routeCollection,
                },
                Values =
                {
                    { "action", "Index" },
                    { "controller", "Home" },
                },
            };
            if (!string.IsNullOrEmpty(currentArea))
            {
                routeData.Values["area"] = currentArea;
            }

            var actionContext = GetActionContext(serviceProvider, routeData);

            var urlHelper = new UrlHelper(actionContext);
            var factory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
            factory
                .Setup(f => f.GetUrlHelper(actionContext))
                .Returns(urlHelper);

            // Make an IUrlHelperFactory available through the ActionContext.
            serviceCollection.AddSingleton<IUrlHelperFactory>(factory.Object);
            serviceProvider = serviceCollection.BuildServiceProvider();
            actionContext.HttpContext.RequestServices = serviceProvider;

            return new ClientModelValidationContext(
                 actionContext,
                 _metadata,
                 _metadataProvider,
                 new AttributeDictionary());
        }

        private static IRouter GetRouteCollectionWithArea(IServiceProvider serviceProvider)
        {
            var builder = GetRouteBuilder(serviceProvider);

            // Setting IsBound to true makes order more important than usual. First try the route that requires the
            // area value. Skip usual "area:exists" constraint because that isn't relevant for link generation and it
            // complicates the setup significantly.
            builder.MapRoute("areaRoute", "{area}/{controller}/{action}");
            builder.MapRoute("default", "{controller}/{action}", new { controller = "Home", action = "Index" });

            return builder.Build();
        }

        private static IRouter GetRouteCollectionWithNoController(IServiceProvider serviceProvider)
        {
            var builder = GetRouteBuilder(serviceProvider);
            builder.MapRoute("default", "static/route");

            return builder.Build();
        }

        private static RouteBuilder GetRouteBuilder(IServiceProvider serviceProvider)
        {
            var app = new Mock<IApplicationBuilder>(MockBehavior.Strict);
            app
                .SetupGet(a => a.ApplicationServices)
                .Returns(serviceProvider);

            var builder = new RouteBuilder(app.Object);

            var handler = new Mock<IRouter>(MockBehavior.Strict);
            handler
                .Setup(router => router.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Returns((VirtualPathData)null);
            builder.DefaultHandler = handler.Object;

            return builder;
        }

        private static ActionContext GetActionContext(IServiceProvider serviceProvider, RouteData routeData)
        {
            // Set IServiceProvider properties because TemplateRoute gets services (e.g. an ILoggerFactory instance)
            // through the HttpContext.
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
            };

            if (routeData == null)
            {
                routeData = new RouteData
                {
                    Routers = { Mock.Of<IRouter>(), },
                };
            }

            return new ActionContext(httpContext, routeData, new ActionDescriptor());
        }

        private static ServiceCollection GetServiceCollection(IStringLocalizerFactory localizerFactory)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            serviceCollection.AddOptions();
            serviceCollection.AddRouting();

            serviceCollection.AddSingleton<IInlineConstraintResolver>(
                provider => new DefaultInlineConstraintResolver(provider.GetRequiredService<IOptions<RouteOptions>>()));

            if (localizerFactory != null)
            {
                serviceCollection.AddSingleton<IStringLocalizerFactory>(localizerFactory);
            }

            return serviceCollection;
        }

        private class MockUrlHelper : IUrlHelper
        {
            private readonly string _routeName;
            private readonly string _url;

            public MockUrlHelper(string url, string routeName)
            {
                _routeName = routeName;
                _url = url;
            }

            public ActionContext ActionContext { get; }

            public object RouteValues { get; private set; }

            public string Action(UrlActionContext actionContext)
            {
                throw new NotImplementedException();
            }

            public string Content(string contentPath)
            {
                throw new NotImplementedException();
            }

            public bool IsLocalUrl(string url)
            {
                throw new NotImplementedException();
            }

            public string Link(string routeName, object values)
            {
                throw new NotImplementedException();
            }

            public string RouteUrl(UrlRouteContext routeContext)
            {
                Assert.Equal(_routeName, routeContext.RouteName);
                Assert.Null(routeContext.Protocol);
                Assert.Null(routeContext.Host);
                Assert.Null(routeContext.Fragment);

                RouteValues = routeContext.Values;

                return _url;
            }
        }

        private class TestableRemoteAttribute : RemoteAttribute
        {
            public TestableRemoteAttribute(string routeName)
                : base(routeName)
            {
            }

            public TestableRemoteAttribute(string action, string controller)
                : base(action, controller)
            {
            }

            public TestableRemoteAttribute(string action, string controller, string areaName)
                : base(action, controller, areaName)
            {
            }

            public new string RouteName
            {
                get
                {
                    return base.RouteName;
                }
            }

            public new RouteValueDictionary RouteData
            {
                get
                {
                    return base.RouteData;
                }
            }
        }
    }
}
