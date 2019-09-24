// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
