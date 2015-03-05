// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.WebEncoders
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
            Assert.Same(HtmlEncoder.Default, serviceProvider.GetRequiredService<IHtmlEncoder>()); // default encoder
            Assert.Same(HtmlEncoder.Default, serviceProvider.GetRequiredService<IHtmlEncoder>()); // as singleton instance
            Assert.Same(JavaScriptStringEncoder.Default, serviceProvider.GetRequiredService<IJavaScriptStringEncoder>()); // default encoder
            Assert.Same(JavaScriptStringEncoder.Default, serviceProvider.GetRequiredService<IJavaScriptStringEncoder>()); // as singleton instance
            Assert.Same(UrlEncoder.Default, serviceProvider.GetRequiredService<IUrlEncoder>()); // default encoder
            Assert.Same(UrlEncoder.Default, serviceProvider.GetRequiredService<IUrlEncoder>()); // as singleton instance
        }

        [Fact]
        public void AddWebEncoders_WithOptions_RegistersEncodersWithCustomCodeFilter()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddWebEncoders(options =>
            {
                options.CodePointFilter = new CodePointFilter().AllowChars("ace"); // only these three chars are allowed
            });

            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var htmlEncoder = serviceProvider.GetRequiredService<IHtmlEncoder>();
            Assert.Equal("a&#x62;c&#x64;e", htmlEncoder.HtmlEncode("abcde"));
            Assert.Same(htmlEncoder, serviceProvider.GetRequiredService<IHtmlEncoder>()); // as singleton instance

            var javaScriptStringEncoder = serviceProvider.GetRequiredService<IJavaScriptStringEncoder>();
            Assert.Equal(@"a\u0062c\u0064e", javaScriptStringEncoder.JavaScriptStringEncode("abcde"));
            Assert.Same(javaScriptStringEncoder, serviceProvider.GetRequiredService<IJavaScriptStringEncoder>()); // as singleton instance

            var urlEncoder = serviceProvider.GetRequiredService<IUrlEncoder>();
            Assert.Equal("a%62c%64e", urlEncoder.UrlEncode("abcde"));
            Assert.Same(urlEncoder, serviceProvider.GetRequiredService<IUrlEncoder>()); // as singleton instance
        }

        [Fact]
        public void AddWebEncoders_DoesNotOverrideExistingRegisteredEncoders()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddSingleton<IHtmlEncoder, CommonTestEncoder>();
            serviceCollection.AddSingleton<IJavaScriptStringEncoder, CommonTestEncoder>();
            // we don't register an existing URL encoder
            serviceCollection.AddWebEncoders(options =>
            {
                options.CodePointFilter = new CodePointFilter().AllowChars("ace"); // only these three chars are allowed
            });

            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var htmlEncoder = serviceProvider.GetHtmlEncoder();
            Assert.Equal("HtmlEncode[[abcde]]", htmlEncoder.HtmlEncode("abcde"));

            var javaScriptStringEncoder = serviceProvider.GetJavaScriptStringEncoder();
            Assert.Equal("JavaScriptStringEncode[[abcde]]", javaScriptStringEncoder.JavaScriptStringEncode("abcde"));

            var urlEncoder = serviceProvider.GetUrlEncoder();
            Assert.Equal("a%62c%64e", urlEncoder.UrlEncode("abcde"));
        }
    }
}
