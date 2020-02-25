// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Net.Http
{
    public class WebAssemblyHttpRequestMessageExtensionsTest
    {
        private const string FetchRequestOptionsKey = "FetchRequestOptions";

        [Theory]
        [InlineData(RequestCredentials.Include, "include")]
        [InlineData(RequestCredentials.Omit, "omit")]
        [InlineData(RequestCredentials.SameOrigin, "same-origin")]
        public void SetRequestCredentials_Works(RequestCredentials requestCredentials, string expected)
        {
            // Arrange
            var requestMessage = new HttpRequestMessage();

            // Act
            requestMessage.SetRequestCredentials(requestCredentials);

            // Assert
            var properties =  Assert.IsAssignableFrom<IDictionary<string, object>>(requestMessage.Properties[FetchRequestOptionsKey]);
            Assert.Collection(
                properties,
                kvp =>
                {
                    Assert.Equal("credentials", kvp.Key);
                    Assert.Equal(expected, kvp.Value);
                });
        }

        [Theory]
        [InlineData(RequestCache.Default, "default")]
        [InlineData(RequestCache.NoStore, "no-store")]
        [InlineData(RequestCache.Reload, "reload")]
        [InlineData(RequestCache.NoCache, "no-cache")]
        [InlineData(RequestCache.ForceCache, "force-cache")]
        [InlineData(RequestCache.OnlyIfCached, "only-if-cached")]
        public void SetRequestCache_Works(RequestCache cache, string expected)
        {
            // Arrange
            var requestMessage = new HttpRequestMessage();

            // Act
            requestMessage.SetRequestCache(cache);

            // Assert
            var properties = Assert.IsAssignableFrom<IDictionary<string, object>>(requestMessage.Properties[FetchRequestOptionsKey]);
            Assert.Collection(
                properties,
                kvp =>
                {
                    Assert.Equal("cache", kvp.Key);
                    Assert.Equal(expected, kvp.Value);
                });
        }

        [Theory]
        [InlineData(RequestMode.SameOrigin, "same-origin")]
        [InlineData(RequestMode.NoCors, "no-cors")]
        [InlineData(RequestMode.Cors, "cors")]
        [InlineData(RequestMode.Navigate, "navigate")]
        public void SetRequestMode_Works(RequestMode mode, string expected)
        {
            // Arrange
            var requestMessage = new HttpRequestMessage();

            // Act
            requestMessage.SetRequestMode(mode);

            // Assert
            var properties = Assert.IsAssignableFrom<IDictionary<string, object>>(requestMessage.Properties[FetchRequestOptionsKey]);
            Assert.Collection(
                properties,
                kvp =>
                {
                    Assert.Equal("mode", kvp.Key);
                    Assert.Equal(expected, kvp.Value);
                });
        }

        [Fact]
        public void SetStreamingEnabled_Works()
        {
            // Arrange
            var requestMessage = new HttpRequestMessage();

            // Act
            requestMessage.SetStreamingEnabled(true);

            // Assert
            Assert.Collection(
                requestMessage.Properties,
                kvp =>
                {
                    Assert.Equal("StreamingEnabled", kvp.Key);
                    Assert.True(Assert.IsType<bool>(kvp.Value));
                });
        }

        [Fact]
        public void SetFetchOptions_Works()
        {
            // Arrange
            var requestMessage = new HttpRequestMessage();

            // Act
            requestMessage.SetFetchOption("some-key", "some-value");

            // Assert
            var properties = Assert.IsAssignableFrom<IDictionary<string, object>>(requestMessage.Properties[FetchRequestOptionsKey]);
            Assert.Collection(
                properties,
                kvp =>
                {
                    Assert.Equal("some-key", kvp.Key);
                    Assert.Equal("some-value", kvp.Value);
                });
        }

        [Fact]
        public void SettingMultipleOptions_Works()
        {
            // Arrange
            var requestMessage = new HttpRequestMessage();

            // Act
            requestMessage.SetRequestCache(RequestCache.ForceCache);
            requestMessage.SetIntegrity("some-value");

            // Assert
            var properties = Assert.IsAssignableFrom<IDictionary<string, object>>(requestMessage.Properties[FetchRequestOptionsKey]);
            Assert.Collection(
                properties.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("cache", kvp.Key);
                    Assert.Equal("force-cache", kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("integrity", kvp.Key);
                    Assert.Equal("some-value", kvp.Value);
                });
        }
    }
}
