// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder.Test
{
    public class ReflectedControllerModelTests
    {
        [Fact]
        public void ReflectedControllerModel_PopulatesAttributes()
        {
            // Arrange
            var controllerType = typeof(BlogController);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.Equal(6, model.Attributes.Count);

            Assert.Single(model.Attributes, a => a is MyOtherAttribute);
            Assert.Single(model.Attributes, a => a is MyFilterAttribute);
            Assert.Single(model.Attributes, a => a is MyRouteConstraintAttribute);
            Assert.Single(model.Attributes, a => a is ApiExplorerSettingsAttribute);

            var routes = model.Attributes.OfType<RouteAttribute>().ToList();
            Assert.Equal(2, routes.Count());
            Assert.Single(routes, r => r.Template.Equals("Blog"));
            Assert.Single(routes, r => r.Template.Equals("Microblog"));
        }

        [Fact]
        public void ReflectedControllerModel_PopulatesFilters()
        {
            // Arrange
            var controllerType = typeof(BlogController);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.Single(model.Filters);
            Assert.IsType<MyFilterAttribute>(model.Filters[0]);
        }

        [Fact]
        public void ReflectedControllerModel_PopulatesRouteConstraintAttributes()
        {
            // Arrange
            var controllerType = typeof(BlogController);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.Single(model.RouteConstraints);
            Assert.IsType<MyRouteConstraintAttribute>(model.RouteConstraints[0]);
        }

        [Fact]
        public void ReflectedControllerModel_ComputesControllerName()
        {
            // Arrange
            var controllerType = typeof(BlogController);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.Equal("Blog", model.ControllerName);
        }

        [Fact]
        public void ReflectedControllerModel_ComputesControllerName_WithoutSuffix()
        {
            // Arrange
            var controllerType = typeof(Store);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.Equal("Store", model.ControllerName);
        }

        [Fact]
        public void ReflectedControllerModel_PopulatesAttributeRouteInfo()
        {
            // Arrange
            var controllerType = typeof(BlogController);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.NotNull(model.AttributeRoutes);
            Assert.Equal(2, model.AttributeRoutes.Count); ;
            Assert.Single(model.AttributeRoutes, r => r.Template.Equals("Blog"));
            Assert.Single(model.AttributeRoutes, r => r.Template.Equals("Microblog"));
        }

        [Fact]
        public void ReflectedControllerModel_PopulatesApiExplorerInfo()
        {
            // Arrange
            var controllerType = typeof(BlogController);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.Equal(true, model.ApiExplorerIsVisible);
            Assert.Equal("Blog", model.ApiExplorerGroupName);
        }

        [Fact]
        public void ReflectedControllerModel_PopulatesApiExplorerInfo_Inherited()
        {
            // Arrange
            var controllerType = typeof(DerivedController);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.Equal(true, model.ApiExplorerIsVisible);
            Assert.Equal("API", model.ApiExplorerGroupName);
        }

        [Fact]
        public void ReflectedControllerModel_PopulatesApiExplorerInfo_NoAttribute()
        {
            // Arrange
            var controllerType = typeof(Store);

            // Act
            var model = new ReflectedControllerModel(controllerType.GetTypeInfo());

            // Assert
            Assert.Null(model.ApiExplorerIsVisible);
            Assert.Null(model.ApiExplorerGroupName);
        }

        [MyOther]
        [MyFilter]
        [MyRouteConstraint]
        [Route("Blog")]
        [Route("Microblog")]
        [ApiExplorerSettings(GroupName = "Blog")]
        private class BlogController
        {
        }

        private class Store
        {
        }

        private class DerivedController : BaseController
        {
        }

        [ApiExplorerSettings(GroupName = "API")]
        private class BaseController
        {

        }

        private class MyRouteConstraintAttribute : RouteConstraintAttribute
        {
            public MyRouteConstraintAttribute()
                : base("MyRouteConstraint", "MyRouteConstraint", false)
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
