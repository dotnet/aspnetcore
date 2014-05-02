// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.TestCommon;

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
            RazorCodeLanguage expected = RazorCodeLanguage.GetLanguageByExtension("cshtml");

            // Act
            RazorCodeLanguage actual = RazorCodeLanguage.GetLanguageByExtension("cshtml");

            // Assert
            Assert.Same(expected, actual);
        }
    }
}
