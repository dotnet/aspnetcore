// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if NET
using System.Buffers;
#endif
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
#if NET
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.Managed;
#endif
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using ExtResources = Microsoft.AspNetCore.DataProtection.Extensions.Resources;

namespace Microsoft.AspNetCore.DataProtection;

public class TimeLimitedDataProtectorTests
{
    private const string TimeLimitedPurposeString = "Microsoft.AspNetCore.DataProtection.TimeLimitedDataProtector.v1";

    [Fact]
    public void Protect_LifetimeSpecified()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        DateTimeOffset expiration = StringToDateTime("2000-01-01 00:00:00Z");
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector("new purpose").CreateProtector(TimeLimitedPurposeString).Protect(
            new byte[] {
                    0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, /* header */
                    0x01, 0x02, 0x03, 0x04, 0x05 /* payload */
            })).Returns(new byte[] { 0x10, 0x11 });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act
        var subProtector = timeLimitedProtector.CreateProtector("new purpose");
        var protectedPayload = subProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, expiration);

        // Assert
        Assert.Equal(new byte[] { 0x10, 0x11 }, protectedPayload);
    }

    [Fact]
    public void Protect_LifetimeNotSpecified_UsesInfiniteLifetime()
    {
        // Arrange
        // 0x2bca2875f4373fff is the representation of DateTimeOffset.MaxValue.
        DateTimeOffset expiration = StringToDateTime("2000-01-01 00:00:00Z");
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector("new purpose").CreateProtector(TimeLimitedPurposeString).Protect(
            new byte[] {
                    0x2b, 0xca, 0x28, 0x75, 0xf4, 0x37, 0x3f, 0xff, /* header */
                    0x01, 0x02, 0x03, 0x04, 0x05 /* payload */
            })).Returns(new byte[] { 0x10, 0x11 });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act
        var subProtector = timeLimitedProtector.CreateProtector("new purpose");
        var protectedPayload = subProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

        // Assert
        Assert.Equal(new byte[] { 0x10, 0x11 }, protectedPayload);
    }

    [Fact]
    public void Unprotect_WithinPayloadValidityPeriod_Success()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        DateTimeOffset expectedExpiration = StringToDateTime("2000-01-01 00:00:00Z");
        DateTimeOffset now = StringToDateTime("1999-01-01 00:00:00Z");
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector(TimeLimitedPurposeString).Unprotect(new byte[] { 0x10, 0x11 })).Returns(
            new byte[] {
                    0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, /* header */
                    0x01, 0x02, 0x03, 0x04, 0x05 /* payload */
            });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act
        var retVal = timeLimitedProtector.UnprotectCore(new byte[] { 0x10, 0x11 }, now, out var actualExpiration);

        // Assert
        Assert.Equal(expectedExpiration, actualExpiration);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, retVal);
    }

    [Fact]
    public void Unprotect_PayloadHasExpired_Fails()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        DateTimeOffset expectedExpiration = StringToDateTime("2000-01-01 00:00:00Z");
        DateTimeOffset now = StringToDateTime("2001-01-01 00:00:00Z");
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector(TimeLimitedPurposeString).Unprotect(new byte[] { 0x10, 0x11 })).Returns(
            new byte[] {
                    0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, /* header */
                    0x01, 0x02, 0x03, 0x04, 0x05 /* payload */
            });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act & assert
        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.UnprotectCore(new byte[] { 0x10, 0x11 }, now, out var _));

        // Assert
        Assert.Equal(ExtResources.FormatTimeLimitedDataProtector_PayloadExpired(expectedExpiration), ex.Message);
    }

    [Fact]
    public void Unprotect_ProtectedDataMalformed_Fails()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector(TimeLimitedPurposeString).Unprotect(new byte[] { 0x10, 0x11 })).Returns(
            new byte[] {
                    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 /* header too short */
            });

        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act & assert
        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.Unprotect(new byte[] { 0x10, 0x11 }, out var _));

        // Assert
        Assert.Equal(ExtResources.TimeLimitedDataProtector_PayloadInvalid, ex.Message);
    }

    [Fact]
    public void Unprotect_UnprotectOperationFails_HomogenizesExceptionToCryptographicException()
    {
        // Arrange
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        var mockInnerProtector = new Mock<IDataProtector>();
        mockInnerProtector.Setup(o => o.CreateProtector(TimeLimitedPurposeString).Unprotect(new byte[] { 0x10, 0x11 })).Throws(new Exception("How exceptional!"));
        var timeLimitedProtector = new TimeLimitedDataProtector(mockInnerProtector.Object);

        // Act & assert
        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.Unprotect(new byte[] { 0x10, 0x11 }, out var _));

        // Assert
        Assert.Equal(Resources.CryptCommon_GenericError, ex.Message);
        Assert.Equal("How exceptional!", ex.InnerException.Message);
    }

    [Fact]
    public void RoundTrip_ProtectedData()
    {
        // Arrange
        var ephemeralProtector = new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("my purpose");
        var timeLimitedProtector = new TimeLimitedDataProtector(ephemeralProtector);
        var expectedExpiration = StringToDateTime("2020-01-01 00:00:00Z");

        // Act
        byte[] ephemeralProtectedPayload = ephemeralProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        byte[] timeLimitedProtectedPayload = timeLimitedProtector.Protect(new byte[] { 0x11, 0x22, 0x33, 0x44 }, expectedExpiration);

        // Assert
        Assert.Equal(
            new byte[] { 0x11, 0x22, 0x33, 0x44 },
            timeLimitedProtector.UnprotectCore(timeLimitedProtectedPayload, StringToDateTime("2010-01-01 00:00:00Z"), out var actualExpiration));
        Assert.Equal(expectedExpiration, actualExpiration);

        // the two providers shouldn't be able to talk to one another (due to the purpose chaining)
        Assert.Throws<CryptographicException>(() => ephemeralProtector.Unprotect(timeLimitedProtectedPayload));
        Assert.Throws<CryptographicException>(() => timeLimitedProtector.Unprotect(ephemeralProtectedPayload, out actualExpiration));
    }

#if NET
    [Fact]
    public void Protect_SpanInnerProtector_WritesHeaderAndPayload()
    {
        // 0x08c1220247e44000 is the representation of midnight 2000-01-01 UTC.
        var inner = new FakeSpanDataProtector(
            protect: plaintext =>
            {
                Assert.Equal(new byte[] { 0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 }, plaintext);
                return new byte[] { 0x10, 0x11 };
            },
            unprotect: null);
        var timeLimitedProtector = new TimeLimitedDataProtector(inner);

        var protectedPayload = timeLimitedProtector.Protect(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, StringToDateTime("2000-01-01 00:00:00Z"));

        Assert.Equal(new byte[] { 0x10, 0x11 }, protectedPayload);
    }

    [Fact]
    public void Unprotect_SpanInnerProtector_WithinPayloadValidityPeriod_Success()
    {
        var inner = new FakeSpanDataProtector(
            protect: null,
            unprotect: protectedData =>
            {
                Assert.Equal(new byte[] { 0x10, 0x11 }, protectedData);
                return new byte[] { 0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
            });
        var timeLimitedProtector = new TimeLimitedDataProtector(inner);

        var retVal = timeLimitedProtector.UnprotectCore(new byte[] { 0x10, 0x11 }, StringToDateTime("1999-01-01 00:00:00Z"), out var actualExpiration);

        Assert.Equal(StringToDateTime("2000-01-01 00:00:00Z"), actualExpiration);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, retVal);
    }

    [Fact]
    public void Unprotect_SpanInnerProtector_PayloadLargerThanProtectedData_Success()
    {
        var payload = Enumerable.Range(0, 10_000).Select(i => (byte)i).ToArray();
        var inner = new FakeSpanDataProtector(
            protect: null,
            unprotect: _ => new byte[] { 0x2b, 0xca, 0x28, 0x75, 0xf4, 0x37, 0x3f, 0xff }.Concat(payload).ToArray());
        var timeLimitedProtector = new TimeLimitedDataProtector(inner);

        Assert.Equal(payload, timeLimitedProtector.Unprotect(new byte[] { 0x10, 0x11 }, out var actualExpiration));
        Assert.Equal(DateTimeOffset.MaxValue, actualExpiration);
    }

    [Fact]
    public void Unprotect_SpanInnerProtector_PayloadHasExpired_Fails()
    {
        var inner = new FakeSpanDataProtector(
            protect: null,
            unprotect: _ => new byte[] { 0x08, 0xc1, 0x22, 0x02, 0x47, 0xe4, 0x40, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });
        var timeLimitedProtector = new TimeLimitedDataProtector(inner);

        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.UnprotectCore(new byte[] { 0x10, 0x11 }, StringToDateTime("2001-01-01 00:00:00Z"), out var _));

        Assert.Equal(ExtResources.FormatTimeLimitedDataProtector_PayloadExpired((DateTimeOffset)StringToDateTime("2000-01-01 00:00:00Z")), ex.Message);
    }

    [Fact]
    public void Unprotect_SpanInnerProtector_ProtectedDataMalformed_Fails()
    {
        var inner = new FakeSpanDataProtector(protect: null, unprotect: _ => new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 });
        var timeLimitedProtector = new TimeLimitedDataProtector(inner);

        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.Unprotect(new byte[] { 0x10, 0x11 }, out var _));

        Assert.Equal(ExtResources.TimeLimitedDataProtector_PayloadInvalid, ex.Message);
    }

    [Fact]
    public void Unprotect_SpanInnerProtector_UnprotectOperationFails_HomogenizesExceptionToCryptographicException()
    {
        var inner = new FakeSpanDataProtector(protect: null, unprotect: _ => throw new Exception("How exceptional!"));
        var timeLimitedProtector = new TimeLimitedDataProtector(inner);

        var ex = Assert.Throws<CryptographicException>(()
            => timeLimitedProtector.Unprotect(new byte[] { 0x10, 0x11 }, out var _));

        Assert.Equal(Resources.CryptCommon_GenericError, ex.Message);
        Assert.Equal("How exceptional!", ex.InnerException.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(100)]
    [InlineData(2048)]
    [InlineData(70000)]
    public void Protect_SpanInnerProtector_ProducesSamePayloadAsByteArrayPath(int length)
    {
        // Known-answer comparison: with a deterministic RNG, the span-based path must produce exactly the same payload
        // as the byte[]-based path, and each path must be able to unprotect the other's payload.
        var genRandom = new CountingGenRandom();
        var kdk = new Secret(Enumerable.Range(0, 64).Select(i => (byte)i).ToArray());
        using var encryptor = new ManagedAuthenticatedEncryptor(kdk, Aes.Create, 32, () => new HMACSHA256(), genRandom: genRandom);

        var keyId = new Guid("ba73c9ce-d322-4e45-af90-341307e11c38");
        var mockKeyRing = new Mock<IKeyRing>();
        mockKeyRing.Setup(o => o.DefaultKeyId).Returns(keyId);
        mockKeyRing.Setup(o => o.DefaultAuthenticatedEncryptor).Returns(encryptor);
        bool revoked;
        mockKeyRing.Setup(o => o.GetAuthenticatedEncryptorByKeyId(keyId, out revoked)).Returns(encryptor);
        var mockKeyRingProvider = new Mock<IKeyRingProvider>();
        mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(mockKeyRing.Object);
        IDataProtector spanInner = new KeyRingBasedSpanDataProtector(mockKeyRingProvider.Object, NullLogger.Instance, originalPurposes: null, newPurpose: "purpose");

        var spanProtector = new TimeLimitedDataProtector(spanInner);
        var byteArrayProtector = new TimeLimitedDataProtector(new NonSpanDataProtector(spanInner));
        var plaintext = Enumerable.Range(0, length).Select(i => (byte)(i * 7)).ToArray();
        var expiration = StringToDateTime("2030-01-01 00:00:00Z");

        genRandom.Reset();
        var expected = byteArrayProtector.Protect(plaintext, expiration);
        genRandom.Reset();
        var actual = spanProtector.Protect(plaintext, expiration);

        Assert.Equal(expected, actual);
        Assert.Equal(plaintext, spanProtector.UnprotectCore(expected, StringToDateTime("2020-01-01 00:00:00Z"), out var spanExpiration));
        Assert.Equal(plaintext, byteArrayProtector.UnprotectCore(actual, StringToDateTime("2020-01-01 00:00:00Z"), out var byteArrayExpiration));
        Assert.Equal(expiration, spanExpiration);
        Assert.Equal(expiration, byteArrayExpiration);
    }

    private sealed class CountingGenRandom : IManagedGenRandom
    {
        private byte _value;

        public void Reset() => _value = 0;

        public byte[] GenRandom(int numBytes)
        {
            var bytes = new byte[numBytes];
            GenRandom(bytes);
            return bytes;
        }

        public void GenRandom(Span<byte> target)
        {
            for (var i = 0; i < target.Length; i++)
            {
                target[i] = _value++;
            }
        }
    }

    // Hides ISpanDataProtector so that TimeLimitedDataProtector takes the byte[]-based path.
    private sealed class NonSpanDataProtector : IDataProtector
    {
        private readonly IDataProtector _inner;

        public NonSpanDataProtector(IDataProtector inner) => _inner = inner;

        public IDataProtector CreateProtector(string purpose) => new NonSpanDataProtector(_inner.CreateProtector(purpose));

        public byte[] Protect(byte[] plaintext) => _inner.Protect(plaintext);

        public byte[] Unprotect(byte[] protectedData) => _inner.Unprotect(protectedData);
    }

    // Writes its output in small chunks to exercise buffer growth; the byte[] overloads must not be used.
    private sealed class FakeSpanDataProtector : ISpanDataProtector
    {
        private readonly Func<byte[], byte[]> _protect;
        private readonly Func<byte[], byte[]> _unprotect;

        public FakeSpanDataProtector(Func<byte[], byte[]> protect, Func<byte[], byte[]> unprotect, string[] purposes = null)
        {
            _protect = protect;
            _unprotect = unprotect;
            Purposes = purposes ?? Array.Empty<string>();
        }

        public string[] Purposes { get; }

        public IDataProtector CreateProtector(string purpose) => new FakeSpanDataProtector(_protect, _unprotect, Purposes.Append(purpose).ToArray());

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

    private static DateTime StringToDateTime(string input)
    {
        return DateTimeOffset.ParseExact(input, "u", CultureInfo.InvariantCulture).UtcDateTime;
    }
}
