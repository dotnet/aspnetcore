// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class NavigationManagerTest
    {
        [Theory]
        [InlineData("scheme://host/", "scheme://host/")]
        [InlineData("scheme://host:123/", "scheme://host:123/")]
        [InlineData("scheme://host/path", "scheme://host/")]
        [InlineData("scheme://host/path/", "scheme://host/path/")]
        [InlineData("scheme://host/path/page?query=string&another=here", "scheme://host/path/")]
        public void ComputesCorrectBaseUri(string baseUri, string expectedResult)
        {
            var actualResult = NavigationManager.NormalizeBaseUri(baseUri);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData("scheme://host/", "scheme://host", "")]
        [InlineData("scheme://host/", "scheme://host/", "")]
        [InlineData("scheme://host/", "scheme://host/path", "path")]
        [InlineData("scheme://host/path/", "scheme://host/path/", "")]
        [InlineData("scheme://host/path/", "scheme://host/path/more", "more")]
        [InlineData("scheme://host/path/", "scheme://host/path", "")]
        [InlineData("scheme://host/path/", "scheme://host/path#hash", "#hash")]
        [InlineData("scheme://host/path/", "scheme://host/path/#hash", "#hash")]
        [InlineData("scheme://host/path/", "scheme://host/path/more#hash", "more#hash")]
        public void ComputesCorrectValidBaseRelativePaths(string baseUri, string uri, string expectedResult)
        {
            var navigationManager = new TestNavigationManager(baseUri);

            var actualResult = navigationManager.ToBaseRelativePath(uri);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData("scheme://host/", "otherscheme://host/")]
        [InlineData("scheme://host/", "scheme://otherhost/")]
        [InlineData("scheme://host/path/", "scheme://host/")]
        public void Initialize_ThrowsForInvalidBaseRelativePaths(string baseUri, string absoluteUri)
        {
            var navigationManager = new TestNavigationManager();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                navigationManager.Initialize(baseUri, absoluteUri);
            });

            Assert.Equal(
                $"The URI '{absoluteUri}' is not contained by the base URI '{baseUri}'.",
                ex.Message);
        }

        [Theory]
        [InlineData("scheme://host/", "otherscheme://host/")]
        [InlineData("scheme://host/", "scheme://otherhost/")]
        [InlineData("scheme://host/path/", "scheme://host/")]
        public void Uri_ThrowsForInvalidBaseRelativePaths(string baseUri, string absoluteUri)
        {
            var navigationManager = new TestNavigationManager(baseUri);

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                navigationManager.ToBaseRelativePath(absoluteUri);
            });

            Assert.Equal(
                $"The URI '{absoluteUri}' is not contained by the base URI '{baseUri}'.",
                ex.Message);
        }

        [Theory]
        [InlineData("scheme://host/", "otherscheme://host/")]
        [InlineData("scheme://host/", "scheme://otherhost/")]
        [InlineData("scheme://host/path/", "scheme://host/")]
        public void ToBaseRelativePath_ThrowsForInvalidBaseRelativePaths(string baseUri, string absoluteUri)
        {
            var navigationManager = new TestNavigationManager(baseUri);

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                navigationManager.ToBaseRelativePath(absoluteUri);
            });

            Assert.Equal(
                $"The URI '{absoluteUri}' is not contained by the base URI '{baseUri}'.",
                ex.Message);
        }

        [Theory]
        [InlineData("scheme://host/?name=Bob%20Joe&age=42", "scheme://host/?name=John%20Doe&age=42")]
        [InlineData("scheme://host/?name=Sally%Smith&age=42&name=Emily", "scheme://host/?name=John%20Doe&age=42&name=John%20Doe")]
        [InlineData("scheme://host/?name=&age=42", "scheme://host/?name=John%20Doe&age=42")]
        [InlineData("scheme://host/?name=", "scheme://host/?name=John%20Doe")]
        public void UriWithQueryParameter_ReplacesWhenParameterExists(string baseUri, string expectedUri)
        {
            var navigationManager = new TestNavigationManager(baseUri);
            var actualUri = navigationManager.UriWithQueryParameter("name", "John Doe");

            Assert.Equal(expectedUri, actualUri);
        }

        [Theory]
        [InlineData("scheme://host/?age=42", "scheme://host/?age=42&name=John%20Doe")]
        [InlineData("scheme://host/", "scheme://host/?name=John%20Doe")]
        [InlineData("scheme://host/?", "scheme://host/?name=John%20Doe")]
        public void UriWithQueryParameter_AppendsWhenParamterDoesNotExist(string baseUri, string expectedUri)
        {
            var navigationManager = new TestNavigationManager(baseUri);
            var actualUri = navigationManager.UriWithQueryParameter("name", "John Doe");

            Assert.Equal(expectedUri, actualUri);
        }

        [Theory]
        [InlineData("scheme://host/?name=Bob%20Joe&age=42", "scheme://host/?age=42")]
        [InlineData("scheme://host/?name=Sally%Smith&age=42&name=Emily", "scheme://host/?age=42")]
        [InlineData("scheme://host/?name=&age=42", "scheme://host/?age=42")]
        [InlineData("scheme://host/?name=", "scheme://host/")]
        public void UriWithQueryParameter_RemovesWhenParameterValueIsNull(string baseUri, string expectedUri)
        {
            var navigationManager = new TestNavigationManager(baseUri);
            var actualUri = navigationManager.UriWithQueryParameter("name", (string)null);

            Assert.Equal(expectedUri, actualUri);
        }

        [Theory]
        [InlineData("scheme://host/?name=Bob%20Joe&age=42", "scheme://host/?age=25&eye-color=green")]
        [InlineData("scheme://host/?age=42&eye-color=87", "scheme://host/?age=25&eye-color=green")]
        [InlineData("scheme://host/?", "scheme://host/?age=25&eye-color=green")]
        [InlineData("scheme://host/", "scheme://host/?age=25&eye-color=green")]
        public void UriWithQueryParameters_CanAddUpdateAndRemove(string baseUri, string expectedUri)
        {
            var navigationManager = new TestNavigationManager(baseUri);
            var actualUri = navigationManager.UriWithQueryParameters(new Dictionary<string, object>
            {
                ["name"] = null,        // Remove
                ["age"] = (int?)25,     // Add/update
                ["eye-color"] = "green",// Add/update
            });

            Assert.Equal(expectedUri, actualUri);
        }

        private class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
            }

            public TestNavigationManager(string baseUri = null, string uri = null)
            {
                Initialize(baseUri ?? "http://example.com/", uri ?? baseUri ?? "http://example.com/welcome-page");
            }

            public new void Initialize(string baseUri, string uri)
            {
                base.Initialize(baseUri, uri);
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
