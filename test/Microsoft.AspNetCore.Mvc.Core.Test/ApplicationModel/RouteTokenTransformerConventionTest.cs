// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Test.ApplicationModel
{
    public class RouteTokenTransformerConventionTest
    {
        [Fact]
        public void Apply_NullAttributeRouteModel_NoOp()
        {
            // Arrange
            var convention = new RouteTokenTransformerConvention(new TestParameterTransformer());

            var model = new ActionModel(GetMethodInfo(), Array.Empty<object>());
            model.Selectors.Add(new SelectorModel()
            {
                AttributeRouteModel = null
            });

            // Act
            convention.Apply(model);

            // Assert
            Assert.Null(model.Selectors[0].AttributeRouteModel);
        }

        [Fact]
        public void Apply_HasAttributeRouteModel_SetRouteTokenTransformer()
        {
            // Arrange
            var transformer = new TestParameterTransformer();
            var convention = new RouteTokenTransformerConvention(transformer);

            var model = new ActionModel(GetMethodInfo(), Array.Empty<object>());
            model.Selectors.Add(new SelectorModel()
            {
                AttributeRouteModel = new AttributeRouteModel()
            });

            // Act
            convention.Apply(model);

            // Assert
            Assert.True(model.Properties.TryGetValue(typeof(IParameterTransformer), out var routeTokenTransformer));
            Assert.Equal(transformer, routeTokenTransformer);
        }

        [Fact]
        public void Apply_ShouldApplyFalse_NoOp()
        {
            // Arrange
            var transformer = new TestParameterTransformer();
            var convention = new CustomRouteTokenTransformerConvention(transformer);

            var model = new ActionModel(GetMethodInfo(), Array.Empty<object>());
            model.Selectors.Add(new SelectorModel()
            {
                AttributeRouteModel = new AttributeRouteModel()
            });

            // Act
            convention.Apply(model);

            // Assert
            Assert.False(model.Properties.TryGetValue(typeof(IParameterTransformer), out _));
        }

        private MethodInfo GetMethodInfo()
        {
            return typeof(RouteTokenTransformerConventionTest).GetMethod(nameof(GetMethodInfo), BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private class TestParameterTransformer : IParameterTransformer
        {
            public string Transform(string value)
            {
                return value;
            }
        }

        private class CustomRouteTokenTransformerConvention : RouteTokenTransformerConvention
        {
            public CustomRouteTokenTransformerConvention(IParameterTransformer parameterTransformer) : base(parameterTransformer)
            {
            }

            protected override bool ShouldApply(ActionModel action)
            {
                return false;
            }
        }
    }
}
