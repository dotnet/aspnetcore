// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Routing;
using System;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Browser.Test
{
    public class UriHelperTest
    {
        [Theory]
        [InlineData("scheme://host/", "scheme://host")]
        [InlineData("scheme://host:123/", "scheme://host:123")]
        [InlineData("scheme://host/path", "scheme://host")]
        [InlineData("scheme://host/path/", "scheme://host/path")]
        [InlineData("scheme://host/path/page?query=string&another=here", "scheme://host/path")]
        public void ComputesCorrectBaseUriPrefix(string baseUri, string expectedResult)
        {
            var actualResult = UriHelper.ToBaseUriPrefix(baseUri);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData("scheme://host", "scheme://host/", "/")]
        [InlineData("scheme://host", "scheme://host/path", "/path")]
        [InlineData("scheme://host/path", "scheme://host/path/", "/")]
        [InlineData("scheme://host/path", "scheme://host/path/more", "/more")]
        [InlineData("scheme://host/path", "scheme://host/path", "/")]
        public void ComputesCorrectValidBaseRelativePaths(string baseUriPrefix, string absoluteUri, string expectedResult)
        {
            var actualResult = UriHelper.ToBaseRelativePath(baseUriPrefix, absoluteUri);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData("scheme://host", "otherscheme://host/")] // Mismatched prefix is error
        [InlineData("scheme://host", "scheme://otherhost/")] // Mismatched prefix is error
        public void ThrowsForInvalidBaseRelativePaths(string baseUriPrefix, string absoluteUri)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                UriHelper.ToBaseRelativePath(baseUriPrefix, absoluteUri);
            });

            Assert.Equal(
                $"The URI '{absoluteUri}' is not contained by the base URI '{baseUriPrefix}'.",
                ex.Message);
        }
    }
}
