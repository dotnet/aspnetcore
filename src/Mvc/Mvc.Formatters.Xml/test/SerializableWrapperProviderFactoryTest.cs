// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class SerializableWrapperProviderFactoryTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Creates_WrapperProvider_ForSerializableErrorType(bool isSerialization)
    {
        // Arrange
        var serializableErrorWrapperProviderFactory = new SerializableErrorWrapperProviderFactory();

        // Act
        var wrapperProvider = serializableErrorWrapperProviderFactory.GetProvider(
                                    new WrapperProviderContext(typeof(SerializableError), isSerialization));

        // Assert
        Assert.NotNull(wrapperProvider);
        Assert.Equal(typeof(SerializableErrorWrapper), wrapperProvider.WrappingType);
    }

    [Fact]
    public void ReturnsNullFor_NonSerializableErrorTypes()
    {
        // Arrange
        var serializableErrorWrapperProviderFactory = new SerializableErrorWrapperProviderFactory();

        // Act
        var wrapperProvider = serializableErrorWrapperProviderFactory.GetProvider(
                                    new WrapperProviderContext(typeof(Person), isSerialization: true));

        // Assert
        Assert.Null(wrapperProvider);
    }
}
