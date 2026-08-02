// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

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

        var expectedMessage = "The object to be wrapped must be of type " +
            $"'{nameof(SerializableErrorWrapper)}' but was of type 'Person'.";

        // Act and Assert
        ExceptionAssert.ThrowsArgument(
            () => wrapperProvider.Wrap(person),
            "original",
            expectedMessage);
    }
}
