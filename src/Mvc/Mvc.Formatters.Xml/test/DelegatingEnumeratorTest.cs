// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
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
}