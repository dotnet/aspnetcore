// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder.Test
{
    public class ReflectedActionModelTests
    {
        [Fact]
        public void ReflectedActionModel_PopulatesAttributes()
        {
            // Arrange
            var actionMethod = typeof(BlogController).GetMethod("Edit");

            // Act
            var model = new ReflectedActionModel(actionMethod);

            // Assert
            Assert.Equal(3, model.Attributes.Count);
            Assert.Single(model.Attributes, a => a is MyFilterAttribute);
            Assert.Single(model.Attributes, a => a is MyOtherAttribute);
            Assert.Single(model.Attributes, a => a is HttpGetAttribute);
        }

        [Fact]
        public void ReflectedActionModel_PopulatesFilters()
        {
            // Arrange
            var actionMethod = typeof(BlogController).GetMethod("Edit");

            // Act
            var model = new ReflectedActionModel(actionMethod);

            // Assert
            Assert.Single(model.Filters);
            Assert.IsType<MyFilterAttribute>(model.Filters[0]);
        }

        private class BlogController
        {
            [MyOther]
            [MyFilter]
            [HttpGet("Edit")]
            public void Edit()
            {
            }
        }

        private class MyFilterAttribute : Attribute, IFilter
        {
        }

        private class MyOtherAttribute : Attribute
        {
        }
    }
}