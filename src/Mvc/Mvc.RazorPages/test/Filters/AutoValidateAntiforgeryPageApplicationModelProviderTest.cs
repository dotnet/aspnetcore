// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    public class AutoValidateAntiforgeryPageApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_AddsFiltersToModel()
        {
            // Arrange
            var actionDescriptor = new PageActionDescriptor();
            var applicationModel = new PageApplicationModel(
                actionDescriptor,
                typeof(object).GetTypeInfo(),
                new object[0]);
            var applicationModelProvider = new AutoValidateAntiforgeryPageApplicationModelProvider();
            var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeof(object).GetTypeInfo())
            {
                PageApplicationModel = applicationModel,
            };

            // Act
            applicationModelProvider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                applicationModel.Filters,
                filter => Assert.IsType<AutoValidateAntiforgeryTokenAttribute>(filter));
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddAutoValidateAntiforgeryTokenAttribute_IfIgnoreAntiforgeryTokenAttributeExists()
        {
            // Arrange
            var expected = new IgnoreAntiforgeryTokenAttribute();

            var descriptor = new PageActionDescriptor();
            var provider = new AutoValidateAntiforgeryPageApplicationModelProvider();
            var context = new PageApplicationModelProviderContext(descriptor, typeof(object).GetTypeInfo())
            {
                PageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>())
                {
                    Filters = { expected },
                },
            };

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.PageApplicationModel.Filters,
                actual => Assert.Same(expected, actual));
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddAutoValidateAntiforgeryTokenAttribute_IfAntiforgeryPolicyExists()
        {
            // Arrange
            var expected = Mock.Of<IAntiforgeryPolicy>();

            var descriptor = new PageActionDescriptor();
            var provider = new AutoValidateAntiforgeryPageApplicationModelProvider();
            var context = new PageApplicationModelProviderContext(descriptor, typeof(object).GetTypeInfo())
            {
                PageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>())
                {
                    Filters = { expected },
                },
            };

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.PageApplicationModel.Filters,
                actual => Assert.Same(expected, actual));
        }
    }
}
