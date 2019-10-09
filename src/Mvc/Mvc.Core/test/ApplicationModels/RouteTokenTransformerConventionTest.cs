// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public class RouteTokenTransformerConventionTest
    {
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
            Assert.Same(transformer, model.RouteParameterTransformer);
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
            Assert.Null(model.RouteParameterTransformer);
        }

        private MethodInfo GetMethodInfo()
        {
            return typeof(RouteTokenTransformerConventionTest).GetMethod(nameof(GetMethodInfo), BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private class TestParameterTransformer : IOutboundParameterTransformer
        {
            public string TransformOutbound(object value)
            {
                return value?.ToString();
            }
        }

        private class CustomRouteTokenTransformerConvention : RouteTokenTransformerConvention
        {
            public CustomRouteTokenTransformerConvention(IOutboundParameterTransformer parameterTransformer) : base(parameterTransformer)
            {
            }

            protected override bool ShouldApply(ActionModel action)
            {
                return false;
            }
        }
    }
}
