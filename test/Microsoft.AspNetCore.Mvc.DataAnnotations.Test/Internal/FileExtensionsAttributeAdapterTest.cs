// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    public class FileExtensionsAttributeAdapterTest
    {
        [Theory]
        [InlineData("jpg,png")]
        [InlineData("jpg, png")]
        [InlineData("JPEG, Png")]
        [ReplaceCulture]
        public void AddValidation_WithoutLocalization(string extensions)
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(Profile), "PhotoFileName");

            var attribute = new FileExtensionsAttribute() { Extensions = extensions };
            attribute.ErrorMessage = "{0} expects only the following extensions: {1}";

            var expectedExtensions = string.Join(", ", extensions.Split(',').Select(s => $".{s.Trim().ToLowerInvariant()}")); 
            var expectedMessage = $"PhotoFileName expects only the following extensions: {expectedExtensions}";

            var adapter = new FileExtensionsAttributeAdapter(attribute, stringLocalizer: null);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider, new AttributeDictionary());

            // Act
            adapter.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-fileextensions", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); },
                kvp => { Assert.Equal("data-val-fileextensions-extensions", kvp.Key); Assert.Equal(extensions.ToLowerInvariant(), kvp.Value); });
        }

        [Fact]
        [ReplaceCulture]
        public void AddValidation_WithLocalization()
        {
            // Arrange
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = provider.GetMetadataForProperty(typeof(Profile), "PhotoFileName");

            var attribute = new FileExtensionsAttribute() { Extensions = "jpg" };
            attribute.ErrorMessage = "{0} expects only the following extensions: {1}";

            var expectedProperties = new object[] { "PhotoFileName", "jpg" };
            var expectedMessage = "PhotoFileName expects only the following extensions: jpg";

            var stringLocalizer = new Mock<IStringLocalizer>();
            stringLocalizer
                .Setup(s => s[attribute.ErrorMessage, expectedProperties])
                .Returns(new LocalizedString(attribute.ErrorMessage, expectedMessage));

            var adapter = new FileExtensionsAttributeAdapter(attribute, stringLocalizer: stringLocalizer.Object);

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider, new AttributeDictionary());

            // Act
            adapter.AddValidation(context);

            // Assert
            Assert.Collection(
                context.Attributes,
                kvp => { Assert.Equal("data-val", kvp.Key); Assert.Equal("true", kvp.Value); },
                kvp => { Assert.Equal("data-val-fileextensions", kvp.Key); Assert.Equal(expectedMessage, kvp.Value); },
                kvp => { Assert.Equal("data-val-fileextensions-extensions", kvp.Key); Assert.Equal("jpg", kvp.Value); });
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

            var actionContext = new ActionContext();
            var context = new ClientModelValidationContext(actionContext, metadata, provider, new AttributeDictionary());

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
}
