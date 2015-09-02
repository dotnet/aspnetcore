// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters.Xml.Internal
{
    public class SerializableErrorWrapperProviderTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Gets_SerializableErrorWrapper_AsWrappingType(bool isSerialization)
        {
            // Arrange
            var wrapperProvider = new SerializableErrorWrapperProvider();

            // Act and Assert
            Assert.Equal(typeof(SerializableErrorWrapper), wrapperProvider.WrappingType);
        }

        [Fact]
        public void Wraps_SerializableErrorInstance()
        {
            // Arrange
            var wrapperProvider = new SerializableErrorWrapperProvider();
            var serializableError = new SerializableError();

            // Act
            var wrapped = wrapperProvider.Wrap(serializableError);

            // Assert
            Assert.NotNull(wrapped);
            var errorWrapper = wrapped as SerializableErrorWrapper;
            Assert.NotNull(errorWrapper);
            Assert.Same(serializableError, errorWrapper.SerializableError);
        }

        [Fact]
        public void ThrowsExceptionOn_NonSerializableErrorInstances()
        {
            // Arrange
            var wrapperProvider = new SerializableErrorWrapperProvider();
            var person = new Person() { Id = 10, Name = "John" };
            var expectedMessage = string.Format("The object to be wrapped must be of type '{0}'" +
                                    $" but was of type 'Person'.{Environment.NewLine}Parameter name: original",
                                    typeof(SerializableErrorWrapper).Name);

            // Act and Assert
            var exception = Assert.Throws<ArgumentException>(() => wrapperProvider.Wrap(person));
            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}