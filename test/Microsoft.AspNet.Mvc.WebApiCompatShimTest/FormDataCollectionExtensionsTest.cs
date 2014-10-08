// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http.Formatting;
using Xunit;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class FormDataCollectionExtensionsTest
    {
        [Theory]
        [InlineData("", null)]
        [InlineData("", "")] // empty 
        [InlineData("x", "x")] // normal key
        [InlineData("", "[]")] // trim []
        [InlineData("x", "x[]")] // trim []
        [InlineData("x[234]", "x[234]")] // array index
        [InlineData("x.y", "x[y]")] // field lookup
        [InlineData("x.y.z", "x[y][z]")] // nested field lookup
        [InlineData("x.y[234].x", "x[y][234][x]")] // compound
        public void TestNormalize(string expectedMvc, string jqueryString)
        {
            Assert.Equal(expectedMvc, FormDataCollectionExtensions.NormalizeJQueryToMvc(jqueryString));
        }

        [Fact]
        public void TestGetJQueryNameValuePairs()
        {
            // Arrange
            var formData = new FormDataCollection("x.y=30&x[y]=70&x[z][20]=cool");

            // Act
            var actual = FormDataCollectionExtensions.GetJQueryNameValuePairs(formData).ToArray();

            // Assert
            var arraySetter = Assert.Single(actual, kvp => kvp.Key == "x.z[20]");
            Assert.Equal("cool", arraySetter.Value);

            Assert.Single(actual, kvp => kvp.Key == "x.y" && kvp.Value == "30");
            Assert.Single(actual, kvp => kvp.Key == "x.y" && kvp.Value == "70");
        }
    }
}
