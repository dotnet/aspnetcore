// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.Extensions.WebEncoders
{
    public class EncoderServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddWebEncoders_WithoutOptions_RegistersDefaultEncoders()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddWebEncoders();

            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.Same(HtmlEncoder.Default, serviceProvider.GetRequiredService<HtmlEncoder>()); // default encoder
            Assert.Same(HtmlEncoder.Default, serviceProvider.GetRequiredService<HtmlEncoder>()); // as singleton instance
            Assert.Same(JavaScriptEncoder.Default, serviceProvider.GetRequiredService<JavaScriptEncoder>()); // default encoder
            Assert.Same(JavaScriptEncoder.Default, serviceProvider.GetRequiredService<JavaScriptEncoder>()); // as singleton instance
            Assert.Same(UrlEncoder.Default, serviceProvider.GetRequiredService<UrlEncoder>()); // default encoder
            Assert.Same(UrlEncoder.Default, serviceProvider.GetRequiredService<UrlEncoder>()); // as singleton instance
        }

        [Fact]
        public void AddWebEncoders_WithOptions_RegistersEncodersWithCustomCodeFilter()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddWebEncoders(options =>
            {
                options.TextEncoderSettings = new TextEncoderSettings();
                options.TextEncoderSettings.AllowCharacters("ace".ToCharArray()); // only these three chars are allowed
            });

            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var htmlEncoder = serviceProvider.GetRequiredService<HtmlEncoder>();
            Assert.Equal("a&#x62;c&#x64;e", htmlEncoder.Encode("abcde"));
            Assert.Same(htmlEncoder, serviceProvider.GetRequiredService<HtmlEncoder>()); // as singleton instance

            var javaScriptEncoder = serviceProvider.GetRequiredService<JavaScriptEncoder>();
            Assert.Equal(@"a\u0062c\u0064e", javaScriptEncoder.Encode("abcde"));
            Assert.Same(javaScriptEncoder, serviceProvider.GetRequiredService<JavaScriptEncoder>()); // as singleton instance

            var urlEncoder = serviceProvider.GetRequiredService<UrlEncoder>();
            Assert.Equal("a%62c%64e", urlEncoder.Encode("abcde"));
            Assert.Same(urlEncoder, serviceProvider.GetRequiredService<UrlEncoder>()); // as singleton instance
        }

        [Fact]
        public void AddWebEncoders_DoesNotOverrideExistingRegisteredEncoders()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddSingleton<HtmlEncoder, HtmlTestEncoder>();
            serviceCollection.AddSingleton<JavaScriptEncoder, JavaScriptTestEncoder>();
            // we don't register an existing URL encoder
            serviceCollection.AddWebEncoders(options =>
            {
                options.TextEncoderSettings = new TextEncoderSettings();
                options.TextEncoderSettings.AllowCharacters("ace".ToCharArray()); // only these three chars are allowed
            });

            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var htmlEncoder = serviceProvider.GetRequiredService<HtmlEncoder>();
            Assert.Equal("HtmlEncode[[abcde]]", htmlEncoder.Encode("abcde"));

            var javaScriptEncoder = serviceProvider.GetRequiredService<JavaScriptEncoder>();
            Assert.Equal("JavaScriptEncode[[abcde]]", javaScriptEncoder.Encode("abcde"));

            var urlEncoder = serviceProvider.GetRequiredService<UrlEncoder>();
            Assert.Equal("a%62c%64e", urlEncoder.Encode("abcde"));
        }
    }
}
