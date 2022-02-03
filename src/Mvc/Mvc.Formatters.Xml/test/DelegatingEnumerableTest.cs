// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class DelegatingEnumerableTest
{
    [Fact]
    public void CanEnumerateOn_NonWrappableElementTypes()
    {
        // Arrange
        var numbers = new[] { 10, 20 };
        var delegatingEnumerable = new DelegatingEnumerable<int, int>(numbers, elementWrapperProvider: null);

        // Act and Assert
        Assert.Equal(numbers, delegatingEnumerable);
    }

    [Fact]
    public void DoesNotThrowOn_EmptyCollections_NonWrappableElementTypes()
    {
        // Arrange
        var numbers = new int[] { };
        var delegatingEnumerable = new DelegatingEnumerable<int, int>(numbers, elementWrapperProvider: null);

        // Act and Assert
        Assert.Empty(delegatingEnumerable);
    }

    [Fact]
    public void CanEnumerateOn_WrappableElementTypes()
    {
        // Arrange
        var error1 = new SerializableError();
        error1.Add("key1", "key1-error");
        var error2 = new SerializableError();
        error2.Add("key1", "key1-error");
        var errors = new[] { error1, error2 };
        var delegatingEnumerable = new DelegatingEnumerable<SerializableErrorWrapper, SerializableError>(
                                                errors,
                                                new SerializableErrorWrapperProvider());

        // Act and Assert
        Assert.Equal(errors.Length, delegatingEnumerable.Count());

        for (var i = 0; i < errors.Length; i++)
        {
            var errorWrapper = delegatingEnumerable.ElementAt(i);

            Assert.IsType<SerializableErrorWrapper>(errorWrapper);
            Assert.NotNull(errorWrapper);
            Assert.Same(errors[i], errorWrapper.SerializableError);
        }
    }

    [Fact]
    public void DoesNotThrowOn_EmptyCollections_WrappableElementTypes()
    {
        // Arrange
        var errors = new SerializableError[] { };
        var delegatingEnumerable = new DelegatingEnumerable<SerializableErrorWrapper, SerializableError>(
                                                errors, new SerializableErrorWrapperProvider());

        // Act and Assert
        Assert.Empty(delegatingEnumerable);
    }
}
