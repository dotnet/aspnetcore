// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class OptionDescriptorExtensionsTest
    {
        [Fact]
        public void InputFormatters_InstanceOf_ThrowsInvalidOperationExceptionIfMoreThanOnceInstance()
        {
            // Arrange
            var formatters = new MvcOptions().InputFormatters;
            formatters.Add(new JsonInputFormatter());
            formatters.Add(Mock.Of<IInputFormatter>());
            formatters.Add(new JsonInputFormatter());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => formatters.InstanceOf<JsonInputFormatter>());
        }

        [Fact]
        public void InputFormatters_InstanceOf_ThrowsInvalidOperationExceptionIfNoInstance()
        {
            // Arrange
            var formatters = new MvcOptions().InputFormatters;
            formatters.Add(Mock.Of<IInputFormatter>());
            formatters.Add(typeof(JsonInputFormatter));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => formatters.InstanceOf<JsonInputFormatter>());
        }

        [Fact]
        public void InputFormatters_InstanceOf_ReturnsInstanceOfIInputFormatterIfOneExists()
        {
            // Arrange
            var formatters = new MvcOptions().InputFormatters;
            formatters.Add(Mock.Of<IInputFormatter>());
            var jsonFormatter = new JsonInputFormatter();
            formatters.Add(jsonFormatter);
            formatters.Add(typeof(JsonInputFormatter));

            // Act
            var formatter = formatters.InstanceOf<JsonInputFormatter>();

            // Assert
            Assert.NotNull(formatter);
            Assert.IsType<JsonInputFormatter>(formatter);
            Assert.Same(jsonFormatter, formatter);
        }

        [Fact]
        public void InputFormatters_InstanceOfOrDefault_ThrowsInvalidOperationExceptionIfMoreThanOnceInstance()
        {
            // Arrange
            var formatters = new MvcOptions().InputFormatters;
            formatters.Add(new JsonInputFormatter());
            formatters.Add(Mock.Of<IInputFormatter>());
            formatters.Add(new JsonInputFormatter());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => formatters.InstanceOfOrDefault<JsonInputFormatter>());
        }

        [Fact]
        public void InputFormatters_InstanceOfOrDefault_ReturnsNullIfNoInstance()
        {
            // Arrange
            var formatters = new MvcOptions().InputFormatters;
            formatters.Add(Mock.Of<IInputFormatter>());
            formatters.Add(typeof(JsonInputFormatter));

            // Act
            var formatter = formatters.InstanceOfOrDefault<JsonInputFormatter>();

            // Assert
            Assert.Null(formatter);
        }

        [Fact]
        public void InputFormatters_InstanceOfOrDefault_ReturnsInstanceOfIInputFormatterIfOneExists()
        {
            // Arrange
            var formatters = new MvcOptions().InputFormatters;
            formatters.Add(Mock.Of<IInputFormatter>());
            formatters.Add(typeof(JsonInputFormatter));
            var jsonFormatter = new JsonInputFormatter();
            formatters.Add(jsonFormatter);

            // Act
            var formatter = formatters.InstanceOfOrDefault<JsonInputFormatter>();

            // Assert
            Assert.NotNull(formatter);
            Assert.IsType<JsonInputFormatter>(formatter);
            Assert.Same(jsonFormatter, formatter);
        }

        [Fact]
        public void InputFormatters_InstancesOf_ReturnsEmptyCollectionIfNoneExist()
        {
            // Arrange
            var formatters = new MvcOptions().InputFormatters;
            formatters.Add(Mock.Of<IInputFormatter>());
            formatters.Add(typeof(JsonInputFormatter));

            // Act
            var jsonFormatters = formatters.InstancesOf<JsonInputFormatter>();

            // Assert
            Assert.Empty(jsonFormatters);
        }

        [Fact]
        public void InputFormatters_InstancesOf_ReturnsNonEmptyCollectionIfSomeExist()
        {
            // Arrange
            var formatters = new MvcOptions().InputFormatters;
            formatters.Add(typeof(JsonInputFormatter));
            var formatter1 = new JsonInputFormatter();
            var formatter2 = Mock.Of<IInputFormatter>();
            var formatter3 = new JsonInputFormatter();
            var formatter4 = Mock.Of<IInputFormatter>();
            formatters.Add(formatter1);
            formatters.Add(formatter2);
            formatters.Add(formatter3);
            formatters.Add(formatter4);

            var expectedFormatters = new List<JsonInputFormatter> { formatter1, formatter3 };

            // Act
            var jsonFormatters = formatters.InstancesOf<JsonInputFormatter>().ToList();

            // Assert
            Assert.NotEmpty(jsonFormatters);
            Assert.Equal(jsonFormatters, expectedFormatters);
        }

        [Fact]
        public void OutputFormatters_InstanceOf_ThrowsInvalidOperationExceptionIfMoreThanOnceInstance()
        {
            // Arrange
            var formatters = new MvcOptions().OutputFormatters;
            formatters.Add(new JsonOutputFormatter());
            formatters.Add(Mock.Of<IOutputFormatter>());
            formatters.Add(new JsonOutputFormatter());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => formatters.InstanceOf<JsonOutputFormatter>());
        }

        [Fact]
        public void OutputFormatters_InstanceOf_ThrowsInvalidOperationExceptionIfNoInstance()
        {
            // Arrange
            var formatters = new MvcOptions().OutputFormatters;
            formatters.Add(Mock.Of<IOutputFormatter>());
            formatters.Add(typeof(JsonOutputFormatter));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => formatters.InstanceOf<JsonOutputFormatter>());
        }

        [Fact]
        public void OutputFormatters_InstanceOf_ReturnsInstanceOfIInputFormatterIfOneExists()
        {
            // Arrange
            var formatters = new MvcOptions().OutputFormatters;
            formatters.Add(Mock.Of<IOutputFormatter>());
            var jsonFormatter = new JsonOutputFormatter();
            formatters.Add(jsonFormatter);
            formatters.Add(typeof(JsonOutputFormatter));

            // Act
            var formatter = formatters.InstanceOf<JsonOutputFormatter>();

            // Assert
            Assert.NotNull(formatter);
            Assert.IsType<JsonOutputFormatter>(formatter);
            Assert.Same(jsonFormatter, formatter);
        }

        [Fact]
        public void OutputFormatters_InstanceOfOrDefault_ThrowsInvalidOperationExceptionIfMoreThanOnceInstance()
        {
            // Arrange
            var formatters = new MvcOptions().OutputFormatters;
            formatters.Add(new JsonOutputFormatter());
            formatters.Add(Mock.Of<IOutputFormatter>());
            formatters.Add(new JsonOutputFormatter());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => formatters.InstanceOfOrDefault<JsonOutputFormatter>());
        }

        [Fact]
        public void OutputFormatters_InstanceOfOrDefault_ReturnsNullIfNoInstance()
        {
            // Arrange
            var formatters = new MvcOptions().OutputFormatters;
            formatters.Add(Mock.Of<IOutputFormatter>());
            formatters.Add(typeof(JsonOutputFormatter));

            // Act
            var formatter = formatters.InstanceOfOrDefault<JsonOutputFormatter>();

            // Assert
            Assert.Null(formatter);
        }

        [Fact]
        public void OutputFormatters_InstanceOfOrDefault_ReturnsInstanceOfIOutputFormatterIfOneExists()
        {
            // Arrange
            var formatters = new MvcOptions().OutputFormatters;
            formatters.Add(Mock.Of<IOutputFormatter>());
            formatters.Add(typeof(JsonOutputFormatter));
            var jsonFormatter = new JsonOutputFormatter();
            formatters.Add(jsonFormatter);

            // Act
            var formatter = formatters.InstanceOfOrDefault<JsonOutputFormatter>();

            // Assert
            Assert.NotNull(formatter);
            Assert.IsType<JsonOutputFormatter>(formatter);
            Assert.Same(jsonFormatter, formatter);
        }

        [Fact]
        public void OutputFormatters_InstancesOf_ReturnsEmptyCollectionIfNoneExist()
        {
            // Arrange
            var formatters = new MvcOptions().OutputFormatters;
            formatters.Add(Mock.Of<IOutputFormatter>());
            formatters.Add(typeof(JsonOutputFormatter));

            // Act
            var jsonFormatters = formatters.InstancesOf<JsonOutputFormatter>();

            // Assert
            Assert.Empty(jsonFormatters);
        }

        [Fact]
        public void OutputFormatters_InstancesOf_ReturnsNonEmptyCollectionIfSomeExist()
        {
            // Arrange
            var formatters = new MvcOptions().OutputFormatters;
            formatters.Add(typeof(JsonOutputFormatter));
            var formatter1 = new JsonOutputFormatter();
            var formatter2 = Mock.Of<IOutputFormatter>();
            var formatter3 = new JsonOutputFormatter();
            var formatter4 = Mock.Of<IOutputFormatter>();
            formatters.Add(formatter1);
            formatters.Add(formatter2);
            formatters.Add(formatter3);
            formatters.Add(formatter4);

            var expectedFormatters = new List<JsonOutputFormatter> { formatter1, formatter3 };

            // Act
            var jsonFormatters = formatters.InstancesOf<JsonOutputFormatter>().ToList();

            // Assert
            Assert.NotEmpty(jsonFormatters);
            Assert.Equal(jsonFormatters, expectedFormatters);
        }
    }
}
