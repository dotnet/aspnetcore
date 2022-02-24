// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class DelegatingEnumeratorTest
{
    [Fact]
    public void DisposeCalled_OnInnerEnumerator()
    {
        // Arrange
        var innerEnumerator = new Mock<IEnumerator<int>>();
        innerEnumerator.Setup(innerEnum => innerEnum.Dispose())
                        .Verifiable();
        var delegatingEnumerator = new DelegatingEnumerator<int, int>(
                                                    innerEnumerator.Object,
                                                    wrapperProvider: null);

        // Act
        delegatingEnumerator.Dispose();

        // Assert
        innerEnumerator.Verify();
    }

    [Fact]
    public void MoveNextCalled_OnInnerEnumerator()
    {
        // Arrange
        var innerEnumerator = new Mock<IEnumerator<int>>();
        innerEnumerator.Setup(innerEnum => innerEnum.MoveNext())
                        .Verifiable();
        var delegatingEnumerator = new DelegatingEnumerator<int, int>(
                                                    innerEnumerator.Object,
                                                    wrapperProvider: null);

        // Act
        var available = delegatingEnumerator.MoveNext();

        // Assert
        innerEnumerator.Verify();
    }

    [Fact]
    public void ResetCalled_OnInnerEnumerator()
    {
        // Arrange
        var innerEnumerator = new Mock<IEnumerator<int>>();
        innerEnumerator.Setup(innerEnum => innerEnum.Reset())
                        .Verifiable();
        var delegatingEnumerator = new DelegatingEnumerator<int, int>(
                                                    innerEnumerator.Object,
                                                    wrapperProvider: null);

        // Act
        delegatingEnumerator.Reset();

        // Assert
        innerEnumerator.Verify();
    }

    [Fact]
    public void CurrentCalled_OnInnerEnumerator()
    {
        // Arrange
        var innerEnumerator = new Mock<IEnumerator<int>>();
        innerEnumerator.SetupGet(innerEnum => innerEnum.Current)
                        .Verifiable();
        var delegatingEnumerator = new DelegatingEnumerator<int, int>(
                                                    innerEnumerator.Object,
                                                    wrapperProvider: null);

        // Act
        var obj = delegatingEnumerator.Current;

        // Assert
        innerEnumerator.Verify();
    }
}
