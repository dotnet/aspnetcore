// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class BindAttributeTest
    {
        [Theory]
        [InlineData("UserName", true)]
        [InlineData("Username", false)]
        [InlineData("Password", false)]
        [InlineData("LastName", true)]
        [InlineData("MiddleName", true)]
        [InlineData(" ", false)]
        [InlineData("foo", true)]
        [InlineData("bar", true)]
        public void BindAttribute_Include(string property, bool isIncluded)
        {
            // Arrange
            var bind = new BindAttribute(new string[] { "UserName", "FirstName", "LastName, MiddleName,  ,foo,bar " });

            var context = new DefaultModelBindingContext();

            var identity = ModelMetadataIdentity.ForProperty(typeof(int), property, typeof(string));
            context.ModelMetadata = new Mock<ModelMetadata>(identity).Object;

            // Act
            var propertyFilter = bind.PropertyFilter;

            // Assert
            Assert.Equal(isIncluded, propertyFilter(context.ModelMetadata));
        }
    }
}
