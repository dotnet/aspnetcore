// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Moq;
using Xunit;

namespace Microsoft.AspNet.DataProtection
{
    public class DataProtectionExtensionsTests
    {
        [Fact]
        public void AsTimeLimitedProtector_ProtectorIsAlreadyTimeLimited_ReturnsThis()
        {
            // Arrange
            var originalProtector = new Mock<ITimeLimitedDataProtector>().Object;

            // Act
            var retVal = originalProtector.AsTimeLimitedDataProtector();

            // Assert
            Assert.Same(originalProtector, retVal);
        }

        [Fact]
        public void AsTimeLimitedProtector_ProtectorIsNotTimeLimited_CreatesNewProtector()
        {
            // Arrange
            var innerProtector = new Mock<IDataProtector>().Object;
            var outerProtectorMock = new Mock<IDataProtector>();
            outerProtectorMock.Setup(o => o.CreateProtector("Microsoft.AspNet.DataProtection.TimeLimitedDataProtector")).Returns(innerProtector);

            // Act
            var timeLimitedProtector = (TimeLimitedDataProtector)outerProtectorMock.Object.AsTimeLimitedDataProtector();

            // Assert
            Assert.Same(innerProtector, timeLimitedProtector.InnerProtector);
        }
    }
}
