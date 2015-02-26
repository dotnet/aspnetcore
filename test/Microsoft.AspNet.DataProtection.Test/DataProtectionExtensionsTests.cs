// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;
using Moq;
using Xunit;

namespace Microsoft.AspNet.DataProtection.Test
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

        [Theory]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { new string[0] })]
        [InlineData(new object[] { new string[] { null } })]
        [InlineData(new object[] { new string[] { "the next value is bad", "" } })]
        public void CreateProtector_Chained_FailureCases(string[] purposes)
        {
            // Arrange
            var mockProtector = new Mock<IDataProtector>();
            mockProtector.Setup(o => o.CreateProtector(It.IsAny<string>())).Returns(mockProtector.Object);
            var provider = mockProtector.Object;

            // Act & assert
            var ex = Assert.Throws<ArgumentException>(() => provider.CreateProtector(purposes));
            ex.AssertMessage("purposes", Resources.DataProtectionExtensions_NullPurposesArray);
        }

        [Fact]
        public void CreateProtector_Chained_SuccessCase()
        {
            // Arrange
            var finalExpectedProtector = new Mock<IDataProtector>().Object;

            var thirdMock = new Mock<IDataProtector>();
            thirdMock.Setup(o => o.CreateProtector("third")).Returns(finalExpectedProtector);
            var secondMock = new Mock<IDataProtector>();
            secondMock.Setup(o => o.CreateProtector("second")).Returns(thirdMock.Object);
            var firstMock = new Mock<IDataProtector>();
            firstMock.Setup(o => o.CreateProtector("first")).Returns(secondMock.Object);

            // Act
            var retVal = firstMock.Object.CreateProtector("first", "second", "third");

            // Assert
            Assert.Same(finalExpectedProtector, retVal);
        }

        [Fact]
        public void Protect_InvalidUtf_Failure()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();

            // Act & assert
            var ex = Assert.Throws<CryptographicException>(() =>
            {
                DataProtectionExtensions.Protect(mockProtector.Object, "Hello\ud800");
            });
            Assert.IsAssignableFrom(typeof(EncoderFallbackException), ex.InnerException);
        }

        [Fact]
        public void Protect_Success()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
            mockProtector.Setup(p => p.Protect(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f })).Returns(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

            // Act
            string retVal = DataProtectionExtensions.Protect(mockProtector.Object, "Hello");

            // Assert
            Assert.Equal("AQIDBAU", retVal);
        }

        [Fact]
        public void Unprotect_InvalidBase64BeforeDecryption_Failure()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();

            // Act & assert
            var ex = Assert.Throws<CryptographicException>(() =>
            {
                DataProtectionExtensions.Unprotect(mockProtector.Object, "A");
            });
            Assert.IsAssignableFrom(typeof(FormatException), ex.InnerException);
        }

        [Fact]
        public void Unprotect_InvalidUtfAfterDecryption_Failure()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
            mockProtector.Setup(p => p.Unprotect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })).Returns(new byte[] { 0xff });

            // Act & assert
            var ex = Assert.Throws<CryptographicException>(() =>
            {
                DataProtectionExtensions.Unprotect(mockProtector.Object, "AQIDBAU");
            });
            Assert.IsAssignableFrom(typeof(DecoderFallbackException), ex.InnerException);
        }

        [Fact]
        public void Unprotect_Success()
        {
            // Arrange
            Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
            mockProtector.Setup(p => p.Unprotect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })).Returns(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f });

            // Act
            string retVal = DataProtectionExtensions.Unprotect(mockProtector.Object, "AQIDBAU");

            // Assert
            Assert.Equal("Hello", retVal);
        }
    }
}
