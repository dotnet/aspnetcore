// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class FilterValuesTest
    {
        [Fact]
        public void IFilter_InitializesCorrectValues()
        {
            // Arrange
            var filter = new TestFilter();

            // Act
            var filterValues = new FilterValues(filter);

            // Assert
            Assert.False(filterValues.IsFactory);
            Assert.Null(filterValues.FilterType);
            Assert.Equal(typeof(TestFilter), filterValues.FilterMetadataType);
            Assert.Equal(
                new List<Type>() { typeof(IFilter), typeof(IExceptionFilter) }, 
                filterValues.FilterInterfaces);
        }

        [Fact]
        public void IFilterFactory_InitializesCorrectValues()
        {
            // Arrange
            var filter = new TestFactory();

            // Act
            var filterValues = new FilterValues(filter);

            // Assert
            Assert.True(filterValues.IsFactory);
            Assert.Null(filterValues.FilterType);
            Assert.Equal(typeof(TestFactory), filterValues.FilterMetadataType);
            Assert.Equal(
                new List<Type>() { typeof(IFilterFactory), typeof(IFilter) },
                filterValues.FilterInterfaces);
        }

        [Fact]
        public void ServiceFilterAttribute_InitializesCorrectValues()
        {
            // Arrange
            var filter = new ServiceFilterAttribute(typeof(TestFilter));

            // Act
            var filterValues = new FilterValues(filter);

            // Assert
            Assert.True(filterValues.IsFactory);
            Assert.Equal(typeof(TestFilter), filterValues.FilterType);
            Assert.Equal(typeof(ServiceFilterAttribute), filterValues.FilterMetadataType);
            Assert.Equal(
                new List<Type>() { typeof(IFilter), typeof(IExceptionFilter) },
                filterValues.FilterInterfaces);
        }

        [Fact]
        public void TypeFilterAttribute_InitializesCorrectValues()
        {
            // Arrange
            var filter = new TypeFilterAttribute(typeof(TestFilter));

            // Act
            var filterValues = new FilterValues(filter);

            // Assert
            Assert.True(filterValues.IsFactory);
            Assert.Equal(typeof(TestFilter), filterValues.FilterType);
            Assert.Equal(typeof(TypeFilterAttribute), filterValues.FilterMetadataType);
            Assert.Equal(
                new List<Type>() { typeof(IFilter), typeof(IExceptionFilter) },
                filterValues.FilterInterfaces);
        }

        private class TestFilter : IFilter, IExceptionFilter
        {
            public void OnException(ExceptionContext context)
            {
            }
        }

        private class TestFactory : IFilterFactory
        {
            public IFilter CreateInstance(IServiceProvider serviceProvider)
            {
                return new TestFilter();
            }
        }
    }
}