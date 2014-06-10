// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class AttributeRouteTemplateTests
    {
        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", null, "")]
        [InlineData(null, "", "")]
        [InlineData("/", null, "")]
        [InlineData(null, "/", "")]
        [InlineData("/", "", "")]
        [InlineData("", "/", "")]
        [InlineData("/", "/", "")]
        [InlineData("/", "/", "")]
        public void Combine_EmptyTemplates(string left, string right, string expected)
        {
            // Arrange & Act
            var combined = AttributeRouteTemplate.Combine(left, right);

            // Assert
            Assert.Equal(expected, combined);
        }

        [Theory]
        [InlineData("home", null, "home")]
        [InlineData("home", "", "home")]
        [InlineData("/home/", "/", "home")]
        [InlineData(null, "GetEmployees", "GetEmployees")]
        [InlineData("/", "GetEmployees", "GetEmployees")]
        [InlineData("", "/GetEmployees/{id}/", "GetEmployees/{id}")]
        public void Combine_OneTemplateHasValue(string left, string right, string expected)
        {
            // Arrange & Act
            var combined = AttributeRouteTemplate.Combine(left, right);

            // Assert
            Assert.Equal(expected, combined);
        }

        [Theory]
        [InlineData("home", "About", "home/About")]
        [InlineData("home/", "/About", "home/About")]
        [InlineData("/home/{action}", "{id}", "home/{action}/{id}")]
        public void Combine_BothTemplatesHasValue(string left, string right, string expected)
        {
            // Arrange & Act
            var combined = AttributeRouteTemplate.Combine(left, right);

            // Assert
            Assert.Equal(expected, combined);
        }
    }
}