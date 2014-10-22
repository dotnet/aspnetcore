// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Razor.Test
{
    public class RazorCodeLanguageTest
    {
        [Fact]
        public void ServicesPropertyContainsEntriesForCSharpCodeLanguageService()
        {
            // Assert
            Assert.Equal(1, RazorCodeLanguage.Languages.Count);
            Assert.IsType<CSharpRazorCodeLanguage>(RazorCodeLanguage.Languages["cshtml"]);
        }

        [Fact]
        public void GetServiceByExtensionReturnsEntryMatchingExtensionWithoutPreceedingDot()
        {
            Assert.IsType<CSharpRazorCodeLanguage>(RazorCodeLanguage.GetLanguageByExtension("cshtml"));
        }

        [Fact]
        public void GetServiceByExtensionReturnsEntryMatchingExtensionWithPreceedingDot()
        {
            Assert.IsType<CSharpRazorCodeLanguage>(RazorCodeLanguage.GetLanguageByExtension(".cshtml"));
        }

        [Fact]
        public void GetServiceByExtensionReturnsNullIfNoServiceForSpecifiedExtension()
        {
            Assert.Null(RazorCodeLanguage.GetLanguageByExtension("foobar"));
        }

        [Fact]
        public void MultipleCallsToGetServiceWithSameExtensionReturnSameObject()
        {
            // Arrange
            var expected = RazorCodeLanguage.GetLanguageByExtension("cshtml");

            // Act
            var actual = RazorCodeLanguage.GetLanguageByExtension("cshtml");

            // Assert
            Assert.Same(expected, actual);
        }
    }
}
