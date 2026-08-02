// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class EnumerableWrapperProviderTest
{
    [Theory]
    [InlineData(typeof(IEnumerable<SerializableError>),
        typeof(DelegatingEnumerable<SerializableErrorWrapper, SerializableError>))]
    [InlineData(typeof(IQueryable<SerializableError>),
        typeof(DelegatingEnumerable<SerializableErrorWrapper, SerializableError>))]
    [InlineData(typeof(ICollection<SerializableError>),
        typeof(DelegatingEnumerable<SerializableErrorWrapper, SerializableError>))]
    [InlineData(typeof(IList<SerializableError>),
        typeof(DelegatingEnumerable<SerializableErrorWrapper, SerializableError>))]
    public void Gets_DelegatingWrappingType(Type declaredEnumerableOfT, Type expectedType)
    {
        // Arrange
        var wrapperProvider = new EnumerableWrapperProvider(
                                                        declaredEnumerableOfT,
                                                        new SerializableErrorWrapperProvider());

        // Act
        var wrappingType = wrapperProvider.WrappingType;

        // Assert
        Assert.NotNull(wrappingType);
        Assert.Equal(expectedType, wrappingType);
    }

    [Fact]
    public void Wraps_EmptyCollections()
    {
        // Arrange
        var declaredEnumerableOfT = typeof(IEnumerable<int>);
        var wrapperProvider = new EnumerableWrapperProvider(
                                            declaredEnumerableOfT,
                                            elementWrapperProvider: null);

        // Act
        var wrapped = wrapperProvider.Wrap(new int[] { });

        // Assert
        Assert.Equal(typeof(DelegatingEnumerable<int, int>), wrapperProvider.WrappingType);
        Assert.NotNull(wrapped);
        var delegatingEnumerable = wrapped as DelegatingEnumerable<int, int>;
        Assert.NotNull(delegatingEnumerable);
        Assert.Empty(delegatingEnumerable);
    }

    [Fact]
    public void Ignores_NullInstances()
    {
        // Arrange
        var declaredEnumerableOfT = typeof(IEnumerable<int>);
        var wrapperProvider = new EnumerableWrapperProvider(
                                    declaredEnumerableOfT,
                                    elementWrapperProvider: null);

        // Act
        var wrapped = wrapperProvider.Wrap(null);

        // Assert
        Assert.Equal(typeof(DelegatingEnumerable<int, int>), wrapperProvider.WrappingType);
        Assert.Null(wrapped);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(List<int>))]
    [InlineData(typeof(List<Person>))]
    [InlineData(typeof(List<SerializableError>))]
    [InlineData(typeof(PersonList))]
    public void ThrowsArgumentExceptionFor_ConcreteEnumerableOfT(Type declaredType)
    {
        // Arrange
        var expectedMessage = "The type must be an interface and must be or derive from 'IEnumerable`1'.";

        // Act and Assert
        ExceptionAssert.ThrowsArgument(() => new EnumerableWrapperProvider(
            declaredType,
            elementWrapperProvider: null),
            "sourceEnumerableOfT",
            expectedMessage);
    }
}
