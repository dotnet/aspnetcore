// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder.Test
{
    public class ReflectedParameterModelTests
    {
        [Fact]
        public void ReflectedParameterModel_PopulatesAttributes()
        {
            // Arrange
            var parameterInfo = typeof(BlogController).GetMethod("Edit").GetParameters()[0];

            // Act
            var model = new ReflectedParameterModel(parameterInfo);

            // Assert
            Assert.Equal(1, model.Attributes.Count);
            Assert.Single(model.Attributes, a => a is MyOtherAttribute);
        }

        [Fact]
        public void ReflectedParameterModel_PopulatesParameterName()
        {
            // Arrange
            var parameterInfo = typeof(BlogController).GetMethod("Edit").GetParameters()[0];

            // Act
            var model = new ReflectedParameterModel(parameterInfo);

            // Assert
            Assert.Equal("name", model.ParameterName);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public void ReflectedParameterModel_PopulatesIsOptional(int parameterIndex, bool expected)
        {
            // Arrange
            var parameterInfo = typeof(BlogController).GetMethod("Edit").GetParameters()[parameterIndex];

            // Act
            var model = new ReflectedParameterModel(parameterInfo);

            // Assert
            Assert.Equal(expected, model.IsOptional);
        }

        private class BlogController
        {
            public void Edit([MyOther] string name, int age = 17)
            {
            }
        }

        private class MyOtherAttribute : Attribute
        {
        }
    }
}