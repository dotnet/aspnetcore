// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class ComplexTypeModelBinderProviderTest
    {
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(List<int>))]
        public void Create_ForNonComplexType_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new ComplexTypeModelBinderProvider();

            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_ForSupportedTypes_ReturnsBinder()
        {
            // Arrange
            var provider = new ComplexTypeModelBinderProvider();

            var context = new TestModelBinderProviderContext(typeof(Person));
            context.OnCreatingBinder(m =>
            {
                if (m.ModelType == typeof(int) || m.ModelType == typeof(string))
                {
                    return Mock.Of<IModelBinder>();
                }
                else
                {
                    Assert.False(true, "Not the right model type");
                    return null;
                }
            });

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<ComplexTypeModelBinder>(result);
        }

        [Theory]
        [InlineData(typeof(PointStructWithExplicitConstructor))]
        [InlineData(typeof(PointStructWithNoExplicitConstructor))]
        public void Create_ForStructModel_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new ComplexTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(typeof(ClassWithNoDefaultConstructor))]
        [InlineData(typeof(ClassWithStaticConstructor))]
        [InlineData(typeof(ClassWithInternalDefaultConstructor))]
        public void Create_ForModelTypeWithNoDefaultPublicConstructor_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new ComplexTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_ForAbstractModelTypeWithDefaultPublicConstructor_ReturnsNull()
        {
            // Arrange
            var provider = new ComplexTypeModelBinderProvider();
            var context = new TestModelBinderProviderContext(typeof(AbstractClassWithDefaultConstructor));

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        private struct PointStructWithNoExplicitConstructor
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        private struct PointStructWithExplicitConstructor
        {
            public PointStructWithExplicitConstructor(double x, double y)
            {
                X = x;
                Y = y;
            }
            public double X { get; }
            public double Y { get; }
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        private class ClassWithNoDefaultConstructor
        {
            public ClassWithNoDefaultConstructor(int id) { }
        }

        private class ClassWithInternalDefaultConstructor
        {
            internal ClassWithInternalDefaultConstructor() { }
        }

        private class ClassWithStaticConstructor
        {
            static ClassWithStaticConstructor() { }

            public ClassWithStaticConstructor(int id) { }
        }

        private abstract class AbstractClassWithDefaultConstructor
        {
            private readonly string _name;

            public AbstractClassWithDefaultConstructor()
                : this("James")
            {
            }

            public AbstractClassWithDefaultConstructor(string name)
            {
                _name = name;
            }
        }
    }
}
