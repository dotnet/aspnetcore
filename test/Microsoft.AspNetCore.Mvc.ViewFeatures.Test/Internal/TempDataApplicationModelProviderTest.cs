// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class TempDataApplicationModelProviderTest
    {
        [Theory]
        [InlineData(typeof(TestController_OneTempDataProperty))]
        [InlineData(typeof(TestController_ListOfString))]
        [InlineData(typeof(TestController_OneNullableTempDataProperty))]
        [InlineData(typeof(TestController_TwoTempDataProperties))]
        [InlineData(typeof(TestController_NullableNonPrimitiveTempDataProperty))]
        public void AddsTempDataPropertyFilter_ForTempDataAttributeProperties(Type type)
        {
            // Arrange
            var provider = new TempDataApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            Assert.Single(controller.Filters, f => f is ControllerSaveTempDataPropertyFilterFactory);
        }

        [Fact]
        public void InitializeFilterFactory_WithExpectedPropertyHelpers_ForTempDataAttributeProperties()
        {
            // Arrange
            var provider = new TempDataApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(TestController_OneTempDataProperty).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act
            provider.OnProvidersExecuting(context);
            var controller = context.Result.Controllers.SingleOrDefault();
            var filter = controller.Filters.OfType<ControllerSaveTempDataPropertyFilterFactory>();
            var saveTempDataPropertyFilterFactory = filter.SingleOrDefault();
            var expected = typeof(TestController_OneTempDataProperty).GetProperty(nameof(TestController_OneTempDataProperty.Test2));

            // Assert
            Assert.NotNull(saveTempDataPropertyFilterFactory);
            var tempDataPropertyHelper = Assert.Single(saveTempDataPropertyFilterFactory.TempDataProperties);
            Assert.Same(expected, tempDataPropertyHelper.PropertyInfo);
        }

        [Fact]
        public void ThrowsInvalidOperationException_PrivateSetter()
        {
            // Arrange
            var provider = new TempDataApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(TestController_PrivateSet).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                provider.OnProvidersExecuting(context));

            Assert.Equal(
                $"The '{typeof(TestController_PrivateSet).FullName}.{nameof(TestController_NonPrimitiveType.Test)}'"
                + $" property with {nameof(TempDataAttribute)} is invalid. A property using {nameof(TempDataAttribute)}"
                + " must have a public getter and setter.",
                exception.Message);
        }

        [Fact]
        public void ThrowsInvalidOperationException_NonPrimitiveType()
        {
            // Arrange
            var provider = new TempDataApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(new[] { typeof(TestController_NonPrimitiveType).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                provider.OnProvidersExecuting(context));

            Assert.Equal(
                $"The '{typeof(TestController_NonPrimitiveType).FullName}.{nameof(TestController_NonPrimitiveType.Test)}'"
                + $" property with {nameof(TempDataAttribute)} is invalid. The '{typeof(TempDataSerializer).FullName}'"
                + $" cannot serialize an object of type '{typeof(Object).FullName}'.",
                exception.Message);
        }

        [Fact]
        public void ThrowsInvalidOperationException_NonStringDictionaryKey()
        {
            // Arrange
            var provider = new TempDataApplicationModelProvider();
            var defaultProvider = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()));

            var context = new ApplicationModelProviderContext(
                new[] { typeof(TestController_NonStringDictionaryKey).GetTypeInfo() });
            defaultProvider.OnProvidersExecuting(context);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                provider.OnProvidersExecuting(context));

            Assert.Equal(
                $"The '{typeof(TestController_NonStringDictionaryKey).FullName}.{nameof(TestController_NonStringDictionaryKey.Test)}'"
                + $" property with {nameof(TempDataAttribute)} is invalid. The '{typeof(TempDataSerializer).FullName}'"
                + $" cannot serialize a dictionary with a key of type '{typeof(Object)}'. The key must be of type"
                + $" '{typeof(string).FullName}'.",
                exception.Message);
        }

        public class TestController_NullableNonPrimitiveTempDataProperty
        {
            [TempData]
            public DateTime? DateTime { get; set; }
        }

        public class TestController_OneTempDataProperty
        {
            public string Test { get; set; }

            [TempData]
            public string Test2 { get; set; }
        }

        public class TestController_TwoTempDataProperties
        {
            [TempData]
            public string Test { get; set; }

            [TempData]
            public int Test2 { get; set; }
        }

        public class TestController_OneNullableTempDataProperty
        {
            public string Test { get; set; }

            [TempData]
            public int? Test2 { get; set; }
        }

        public class TestController_ListOfString
        {
            [TempData]
            public IList<string> Test { get; set; }
        }

        public class TestController_PrivateSet
        {
            [TempData]
            public string Test { get; private set; }
        }

        public class TestController_NonPrimitiveType
        {
            [TempData]
            public object Test { get; set; }
        }

        public class TestController_NonStringDictionaryKey
        {
            [TempData]
            public IDictionary<object, object> Test { get; set; }
        }
    }
}
