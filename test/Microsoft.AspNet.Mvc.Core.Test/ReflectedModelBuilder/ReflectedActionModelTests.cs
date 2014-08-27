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

        [Fact]
        public void ReflectedActionModel_PopulatesApiExplorerInfo()
        {
            // Arrange
            var actionMethod = typeof(BlogController).GetMethod("Create");

            // Act
            var model = new ReflectedActionModel(actionMethod);

            // Assert
            Assert.Equal(false, model.ApiExplorerIsVisible);
            Assert.Equal("Blog", model.ApiExplorerGroupName);
        }

        [Fact]
        public void ReflectedActionModel_PopulatesApiExplorerInfo_NoAttribute()
        {
            // Arrange
            var actionMethod = typeof(BlogController).GetMethod("Edit");

            // Act
            var model = new ReflectedActionModel(actionMethod);

            // Assert
            Assert.Null(model.ApiExplorerIsVisible);
            Assert.Null(model.ApiExplorerGroupName);
        }

        private class BlogController
        {
            [MyOther]
            [MyFilter]
            [HttpGet("Edit")]
            public void Edit()
            {
            }

            [ApiExplorerSettings(IgnoreApi = true, GroupName = "Blog")]
            public void Create()
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
