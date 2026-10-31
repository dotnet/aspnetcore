// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection.Abstractions;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection;

public class DataProtectionCommonExtensionsTests
{
    [Theory]
    [InlineData(new object[] { new string[0] })]
    [InlineData(new object[] { new string[] { null } })]
    [InlineData(new object[] { new string[] { "the next value is bad", null } })]
    public void CreateProtector_ChainedAsIEnumerable_FailureCases(string[] purposes)
    {
        // Arrange
        var mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(o => o.CreateProtector(It.IsAny<string>())).Returns(mockProtector.Object);
        var provider = mockProtector.Object;

        // Act & assert
        ExceptionAssert.ThrowsArgument(
            testCode: () => provider.CreateProtector((IEnumerable<string>)purposes),
            paramName: "purposes",
            exceptionMessage: Resources.DataProtectionExtensions_NullPurposesCollection);
    }

    [Theory]
    [InlineData(new object[] { new string[] { null } })]
    [InlineData(new object[] { new string[] { "the next value is bad", null } })]
    public void CreateProtector_ChainedAsParams_FailureCases(string[] subPurposes)
    {
        // Arrange
        var mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(o => o.CreateProtector(It.IsAny<string>())).Returns(mockProtector.Object);
        var provider = mockProtector.Object;

        // Act & assert
        ExceptionAssert.ThrowsArgument(
            testCode: () => provider.CreateProtector("primary-purpose", subPurposes),
            paramName: "purposes",
            exceptionMessage: Resources.DataProtectionExtensions_NullPurposesCollection);
    }

    [Fact]
    public void CreateProtector_ChainedAsIEnumerable_SuccessCase()
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
        var retVal = firstMock.Object.CreateProtector((IEnumerable<string>)new string[] { "first", "second", "third" });

        // Assert
        Assert.Same(finalExpectedProtector, retVal);
    }

    [Fact]
    public void CreateProtector_ChainedAsParams_NonEmptyParams_SuccessCase()
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

    [Theory]
    [InlineData(new object[] { null })]
    [InlineData(new object[] { new string[0] })]
    public void CreateProtector_ChainedAsParams_EmptyParams_SuccessCases(string[] subPurposes)
    {
        // Arrange
        var finalExpectedProtector = new Mock<IDataProtector>().Object;
        var firstMock = new Mock<IDataProtector>();
        firstMock.Setup(o => o.CreateProtector("first")).Returns(finalExpectedProtector);

        // Act
        var retVal = firstMock.Object.CreateProtector("first", subPurposes);

        // Assert
        Assert.Same(finalExpectedProtector, retVal);
    }

    [Fact]
    public void GetDataProtectionProvider_NoServiceFound_Throws()
    {
        // Arrange
        var services = new Mock<IServiceProvider>().Object;

        // Act & assert
        var ex = Assert.Throws<InvalidOperationException>(() => services.GetDataProtectionProvider());
        Assert.Equal(Resources.FormatDataProtectionExtensions_NoService(typeof(IDataProtectionProvider).FullName), ex.Message);
    }

    [Fact]
    public void GetDataProtectionProvider_ServiceFound_ReturnsService()
    {
        // Arrange
        var expected = new Mock<IDataProtectionProvider>().Object;
        var mockServices = new Mock<IServiceProvider>();
        mockServices.Setup(o => o.GetService(typeof(IDataProtectionProvider))).Returns(expected);
        var services = mockServices.Object;

        // Act
        var actual = services.GetDataProtectionProvider();

        // Assert
        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(new object[] { new string[0] })]
    [InlineData(new object[] { new string[] { null } })]
    [InlineData(new object[] { new string[] { "the next value is bad", null } })]
    public void GetDataProtector_ChainedAsIEnumerable_FailureCases(string[] purposes)
    {
        // Arrange
        var mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(o => o.CreateProtector(It.IsAny<string>())).Returns(mockProtector.Object);
        var mockServices = new Mock<IServiceProvider>();
        mockServices.Setup(o => o.GetService(typeof(IDataProtectionProvider))).Returns(mockProtector.Object);
        var services = mockServices.Object;

        // Act & assert
        ExceptionAssert.ThrowsArgument(
            testCode: () => services.GetDataProtector((IEnumerable<string>)purposes),
            paramName: "purposes",
            exceptionMessage: Resources.DataProtectionExtensions_NullPurposesCollection);
    }

    [Theory]
    [InlineData(new object[] { new string[] { null } })]
    [InlineData(new object[] { new string[] { "the next value is bad", null } })]
    public void GetDataProtector_ChainedAsParams_FailureCases(string[] subPurposes)
    {
        // Arrange
        var mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(o => o.CreateProtector(It.IsAny<string>())).Returns(mockProtector.Object);
        var mockServices = new Mock<IServiceProvider>();
        mockServices.Setup(o => o.GetService(typeof(IDataProtectionProvider))).Returns(mockProtector.Object);
        var services = mockServices.Object;

        // Act & assert
        ExceptionAssert.ThrowsArgument(
            testCode: () => services.GetDataProtector("primary-purpose", subPurposes),
            paramName: "purposes",
            exceptionMessage: Resources.DataProtectionExtensions_NullPurposesCollection);
    }

    [Fact]
    public void GetDataProtector_ChainedAsIEnumerable_SuccessCase()
    {
        // Arrange
        var finalExpectedProtector = new Mock<IDataProtector>().Object;

        var thirdMock = new Mock<IDataProtector>();
        thirdMock.Setup(o => o.CreateProtector("third")).Returns(finalExpectedProtector);
        var secondMock = new Mock<IDataProtector>();
        secondMock.Setup(o => o.CreateProtector("second")).Returns(thirdMock.Object);
        var firstMock = new Mock<IDataProtector>();
        firstMock.Setup(o => o.CreateProtector("first")).Returns(secondMock.Object);

        var mockServices = new Mock<IServiceProvider>();
        mockServices.Setup(o => o.GetService(typeof(IDataProtectionProvider))).Returns(firstMock.Object);
        var services = mockServices.Object;

        // Act
        var retVal = services.GetDataProtector((IEnumerable<string>)new string[] { "first", "second", "third" });

        // Assert
        Assert.Same(finalExpectedProtector, retVal);
    }

    [Fact]
    public void GetDataProtector_ChainedAsParams_NonEmptyParams_SuccessCase()
    {
        // Arrange
        var finalExpectedProtector = new Mock<IDataProtector>().Object;

        var thirdMock = new Mock<IDataProtector>();
        thirdMock.Setup(o => o.CreateProtector("third")).Returns(finalExpectedProtector);
        var secondMock = new Mock<IDataProtector>();
        secondMock.Setup(o => o.CreateProtector("second")).Returns(thirdMock.Object);
        var firstMock = new Mock<IDataProtector>();
        firstMock.Setup(o => o.CreateProtector("first")).Returns(secondMock.Object);

        var mockServices = new Mock<IServiceProvider>();
        mockServices.Setup(o => o.GetService(typeof(IDataProtectionProvider))).Returns(firstMock.Object);
        var services = mockServices.Object;

        // Act
        var retVal = services.GetDataProtector("first", "second", "third");

        // Assert
        Assert.Same(finalExpectedProtector, retVal);
    }

    [Theory]
    [InlineData(new object[] { null })]
    [InlineData(new object[] { new string[0] })]
    public void GetDataProtector_ChainedAsParams_EmptyParams_SuccessCases(string[] subPurposes)
    {
        // Arrange
        var finalExpectedProtector = new Mock<IDataProtector>().Object;
        var firstMock = new Mock<IDataProtector>();
        firstMock.Setup(o => o.CreateProtector("first")).Returns(finalExpectedProtector);
        var mockServices = new Mock<IServiceProvider>();
        mockServices.Setup(o => o.GetService(typeof(IDataProtectionProvider))).Returns(firstMock.Object);
        var services = mockServices.Object;

        // Act
        var retVal = services.GetDataProtector("first", subPurposes);

        // Assert
        Assert.Same(finalExpectedProtector, retVal);
    }

    [Fact]
    public void Protect_InvalidUtf8_Failure()
    {
        // Arrange
        Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();

        // Act & assert
        var ex = Assert.Throws<CryptographicException>(() =>
        {
            mockProtector.Object.Protect("Hello\ud800");
        });
        Assert.IsAssignableFrom<EncoderFallbackException>(ex.InnerException);
    }

    [Fact]
    public void Protect_Success()
    {
        // Arrange
        Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(p => p.Protect(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f })).Returns(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

        // Act
        string retVal = mockProtector.Object.Protect("Hello");

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
            mockProtector.Object.Unprotect("A");
        });
    }

    [Fact]
    public void Unprotect_InvalidUtf8AfterDecryption_Failure()
    {
        // Arrange
        Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(p => p.Unprotect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })).Returns(new byte[] { 0xff });

        // Act & assert
        var ex = Assert.Throws<CryptographicException>(() =>
        {
            mockProtector.Object.Unprotect("AQIDBAU");
        });
        Assert.IsAssignableFrom<DecoderFallbackException>(ex.InnerException);
    }

    [Fact]
    public void Unprotect_Success()
    {
        // Arrange
        Mock<IDataProtector> mockProtector = new Mock<IDataProtector>();
        mockProtector.Setup(p => p.Unprotect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })).Returns(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f });

        // Act
        string retVal = DataProtectionCommonExtensions.Unprotect(mockProtector.Object, "AQIDBAU");

        // Assert
        Assert.Equal("Hello", retVal);
    }
#if NET
    [Fact]
    public void Protect_SpanProtector_UsesSpanPath_Success()
    {
        var protector = new FakeSpanDataProtector(
            protect: plaintext => { Assert.Equal(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f }, plaintext); return new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }; },
            unprotect: null);

        Assert.Equal("AQIDBAU", protector.Protect("Hello"));
    }

    [Fact]
    public void Protect_SpanProtector_EmptyString_Success()
    {
        var protector = new FakeSpanDataProtector(
            protect: plaintext => { Assert.Empty(plaintext); return new byte[] { 0x01 }; },
            unprotect: null);

        Assert.Equal("AQ", protector.Protect(""));
    }

    [Fact]
    public void Protect_SpanProtector_LargeOutput_GrowsBuffer()
    {
        var output = new byte[100_000];
        new Random(42).NextBytes(output);
        var protector = new FakeSpanDataProtector(protect: _ => output, unprotect: null);

        Assert.Equal(Convert.ToBase64String(output).TrimEnd('=').Replace('+', '-').Replace('/', '_'), protector.Protect("x"));
    }

    [Fact]
    public void Protect_SpanProtector_InvalidUtf8_Failure()
    {
        var protector = new FakeSpanDataProtector(protect: null, unprotect: null);

        var ex = Assert.Throws<CryptographicException>(() => protector.Protect("Hello\ud800"));
        Assert.IsAssignableFrom<EncoderFallbackException>(ex.InnerException);
    }

    [Fact]
    public void Protect_SpanProtector_ProtectorThrows_ExceptionPropagates()
    {
        var expected = new CryptographicException("from protector");
        var protector = new FakeSpanDataProtector(protect: _ => throw expected, unprotect: null);

        Assert.Same(expected, Assert.Throws<CryptographicException>(() => protector.Protect("Hello")));
    }

    [Fact]
    public void Unprotect_SpanProtector_UsesSpanPath_Success()
    {
        var protector = new FakeSpanDataProtector(
            protect: null,
            unprotect: protectedData => { Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, protectedData); return new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f }; });

        Assert.Equal("Hello", protector.Unprotect("AQIDBAU"));
    }

    [Fact]
    public void Unprotect_SpanProtector_PlaintextLargerThanPayload_GrowsBuffer()
    {
        var plaintext = new string('a', 50_000) + "\u00fc";
        var protector = new FakeSpanDataProtector(protect: null, unprotect: _ => Encoding.UTF8.GetBytes(plaintext));

        Assert.Equal(plaintext, protector.Unprotect("AQIDBAU"));
    }

    [Fact]
    public void Unprotect_SpanProtector_InvalidBase64BeforeDecryption_Failure()
    {
        var protector = new FakeSpanDataProtector(protect: null, unprotect: null);

        Assert.Throws<CryptographicException>(() => protector.Unprotect("A"));
    }

    [Fact]
    public void Unprotect_SpanProtector_InvalidUtf8AfterDecryption_Failure()
    {
        var protector = new FakeSpanDataProtector(protect: null, unprotect: _ => new byte[] { 0xff });

        var ex = Assert.Throws<CryptographicException>(() => protector.Unprotect("AQIDBAU"));
        Assert.IsAssignableFrom<DecoderFallbackException>(ex.InnerException);
    }

    // Writes its output in small chunks to exercise buffer growth; the byte[] overloads must not be used.
    private sealed class FakeSpanDataProtector : ISpanDataProtector
    {
        private readonly Func<byte[], byte[]> _protect;
        private readonly Func<byte[], byte[]> _unprotect;

        public FakeSpanDataProtector(Func<byte[], byte[]> protect, Func<byte[], byte[]> unprotect)
        {
            _protect = protect;
            _unprotect = unprotect;
        }

        public IDataProtector CreateProtector(string purpose) => throw new NotSupportedException();

        public byte[] Protect(byte[] plaintext) => throw new InvalidOperationException("The byte[] overload should not be called.");

        public byte[] Unprotect(byte[] protectedData) => throw new InvalidOperationException("The byte[] overload should not be called.");

        public void Protect<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct
            => WriteInChunks(_protect(plaintext.ToArray()), ref destination);

        public void Unprotect<TWriter>(ReadOnlySpan<byte> protectedData, ref TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct
            => WriteInChunks(_unprotect(protectedData.ToArray()), ref destination);

        private static void WriteInChunks<TWriter>(byte[] data, ref TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct
        {
            for (var offset = 0; offset < data.Length;)
            {
                var chunk = Math.Min(4096, data.Length - offset);
                var span = destination.GetSpan(chunk);
                data.AsSpan(offset, chunk).CopyTo(span);
                destination.Advance(chunk);
                offset += chunk;
            }
        }
    }
#endif
}
