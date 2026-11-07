// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.ObjectPool;
using Moq;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class DefaultAntiforgeryTokenSerializerTest
{
    private static readonly BinaryBlob _claimUid = new BinaryBlob(256, [0x6F, 0x16, 0x48, 0xE9, 0x72, 0x49, 0xAA, 0x58, 0x75, 0x40, 0x36, 0xA6, 0x7E, 0x24, 0x8C, 0xF0, 0x44, 0xF0, 0x7E, 0xCF, 0xB0, 0xED, 0x38, 0x75, 0x56, 0xCE, 0x02, 0x9A, 0x4F, 0x9A, 0x40, 0xE0]);
    private static readonly BinaryBlob _securityToken = new BinaryBlob(128, [0x70, 0x5E, 0xED, 0xCC, 0x7D, 0x42, 0xF1, 0xD6, 0xB3, 0xB9, 0x8A, 0x59, 0x36, 0x25, 0xBB, 0x4C]);
    private const byte _salt = 0x05;

    public enum DataProtectorType
    {
        /// <summary>
        /// Uses <see cref="ISpanDataProtector"/> - the optimized path.
        /// </summary>
        SpanDataProtector,

        /// <summary>
        /// Uses plain <see cref="IDataProtector"/> - the fallback path.
        /// </summary>
        DefaultDataProtector
    }

    [Theory]
    [InlineData(DataProtectorType.SpanDataProtector,
        "01" // Version
        + "705EEDCC7D42F1D6B3B9" // SecurityToken
                                 // (WRONG!) Stream ends too early
        )]
    [InlineData(DataProtectorType.DefaultDataProtector,
        "01" // Version
        + "705EEDCC7D42F1D6B3B9" // SecurityToken
                                 // (WRONG!) Stream ends too early
        )]
    [InlineData(DataProtectorType.SpanDataProtector,
        "01" // Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "01" // IsCookieToken
        + "00" // (WRONG!) Too much data in stream
        )]
    [InlineData(DataProtectorType.DefaultDataProtector,
        "01" // Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "01" // IsCookieToken
        + "00" // (WRONG!) Too much data in stream
        )]
    [InlineData(DataProtectorType.SpanDataProtector,
        "02" // (WRONG! - must be 0x01) Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "01" // IsCookieToken
        )]
    [InlineData(DataProtectorType.DefaultDataProtector,
        "02" // (WRONG! - must be 0x01) Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "01" // IsCookieToken
        )]
    [InlineData(DataProtectorType.SpanDataProtector,
        "01" // Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "00" // IsCookieToken
        + "00" // IsClaimsBased
        + "05" // Username length header
        + "0000" // (WRONG!) Too little data in stream
        )]
    [InlineData(DataProtectorType.DefaultDataProtector,
        "01" // Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "00" // IsCookieToken
        + "00" // IsClaimsBased
        + "05" // Username length header
        + "0000" // (WRONG!) Too little data in stream
        )]
    public void Deserialize_BadToken_Throws(DataProtectorType protectorType, string serializedToken)
    {
        // Arrange
        var dataProtector = CreateDataProtector(protectorType);
        var testSerializer = new DefaultAntiforgeryTokenSerializer(dataProtector.Object);

        // Act & assert
        var ex = Assert.Throws<AntiforgeryValidationException>(() => testSerializer.Deserialize(serializedToken));
        Assert.Equal(@"The antiforgery token could not be decrypted.", ex.Message);
    }

    [Theory]
    [InlineData(DataProtectorType.SpanDataProtector, "a=")]
    [InlineData(DataProtectorType.DefaultDataProtector, "a=")]
    [InlineData(DataProtectorType.SpanDataProtector, "ab==")]
    [InlineData(DataProtectorType.DefaultDataProtector, "ab==")]
    [InlineData(DataProtectorType.SpanDataProtector, "abcd=")]
    [InlineData(DataProtectorType.DefaultDataProtector, "abcd=")]
    [InlineData(DataProtectorType.SpanDataProtector, "ab+d")]
    [InlineData(DataProtectorType.DefaultDataProtector, "ab+d")]
    [InlineData(DataProtectorType.SpanDataProtector, "ab/d")]
    [InlineData(DataProtectorType.DefaultDataProtector, "ab/d")]
    [InlineData(DataProtectorType.SpanDataProtector, "!")]
    [InlineData(DataProtectorType.DefaultDataProtector, "!")]
    [InlineData(DataProtectorType.SpanDataProtector, "a b")]
    [InlineData(DataProtectorType.DefaultDataProtector, "a b")]
    [InlineData(DataProtectorType.SpanDataProtector, "")]
    [InlineData(DataProtectorType.DefaultDataProtector, "")]
    [InlineData(DataProtectorType.SpanDataProtector, "a")]
    [InlineData(DataProtectorType.DefaultDataProtector, "a")]
    public void Deserialize_InvalidBase64UrlInput_ThrowsAndWrapsFormatException(DataProtectorType protectorType, string serializedToken)
    {
        var unprotectCalled = false;
        var dataProtector = CreateDataProtector(protectorType, () => unprotectCalled = true);
        var serializer = new DefaultAntiforgeryTokenSerializer(dataProtector.Object);

        var ex = Assert.Throws<AntiforgeryValidationException>(() => serializer.Deserialize(serializedToken));
        Assert.Equal("The antiforgery token could not be decrypted.", ex.Message);
        Assert.IsType<FormatException>(ex.InnerException);
        Assert.False(unprotectCalled);
    }

    [Theory]
    [InlineData(DataProtectorType.SpanDataProtector)]
    [InlineData(DataProtectorType.DefaultDataProtector)]
    public void Deserialize_ValidTokenWithInvalidBase64UrlSuffix_RejectsBeforeUnprotect(DataProtectorType protectorType)
    {
        var unprotectCalled = false;
        var dataProtector = CreateDataProtector(protectorType, () => unprotectCalled = true);
        var serializer = new DefaultAntiforgeryTokenSerializer(dataProtector.Object);
        var token = new AntiforgeryToken()
        {
            SecurityToken = _securityToken,
            IsCookieToken = false,
            Username = "ab"
        };
        // Complete Base64Url blocks let the decoder write the entire token before the invalid suffix.
        var serializedToken = serializer.Serialize(token) + "!";

        var ex = Assert.Throws<AntiforgeryValidationException>(() => serializer.Deserialize(serializedToken));
        Assert.Equal("The antiforgery token could not be decrypted.", ex.Message);
        Assert.IsType<FormatException>(ex.InnerException);
        Assert.False(unprotectCalled);
    }

    [Theory]
    [InlineData(DataProtectorType.SpanDataProtector, "==")]
    [InlineData(DataProtectorType.DefaultDataProtector, "==")]
    [InlineData(DataProtectorType.SpanDataProtector, " ")]
    [InlineData(DataProtectorType.DefaultDataProtector, " ")]
    public void Deserialize_ValidTokenWithPaddingOrWhitespace_TokenRoundTripSuccessful(DataProtectorType protectorType, string suffix)
    {
        var dataProtector = CreateDataProtector(protectorType);
        var serializer = new DefaultAntiforgeryTokenSerializer(dataProtector.Object);
        var token = new AntiforgeryToken()
        {
            SecurityToken = _securityToken,
            IsCookieToken = true
        };
        var serializedToken = serializer.Serialize(token) + suffix;

        var deserializedToken = serializer.Deserialize(serializedToken);

        AssertTokensEqual(token, deserializedToken);
    }

    [Theory]
    [InlineData(DataProtectorType.SpanDataProtector)]
    [InlineData(DataProtectorType.DefaultDataProtector)]
    public void Deserialize_UnprotectThrows_PreservesInnerException(DataProtectorType protectorType)
    {
        var expectedException = new InvalidOperationException("Unprotect failed.");
        var dataProtector = CreateDataProtector(protectorType, () => throw expectedException);
        var serializer = new DefaultAntiforgeryTokenSerializer(dataProtector.Object);
        var token = new AntiforgeryToken()
        {
            SecurityToken = _securityToken,
            IsCookieToken = true
        };
        var serializedToken = serializer.Serialize(token);

        var ex = Assert.Throws<AntiforgeryValidationException>(() => serializer.Deserialize(serializedToken));

        Assert.Equal("The antiforgery token could not be decrypted.", ex.Message);
        Assert.Same(expectedException, ex.InnerException);
    }

    [Theory]
    [InlineData(DataProtectorType.SpanDataProtector)]
    [InlineData(DataProtectorType.DefaultDataProtector)]
    public void Serialize_FieldToken_WithClaimUid_TokenRoundTripSuccessful(DataProtectorType protectorType)
    {
        // Arrange
        var dataProtector = CreateDataProtector(protectorType);
        var testSerializer = new DefaultAntiforgeryTokenSerializer(dataProtector.Object);

        //"01" // Version
        //+ "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        //+ "00" // IsCookieToken
        //+ "01" // IsClaimsBased
        //+ "6F1648E97249AA58754036A67E248CF044F07ECFB0ED387556CE029A4F9A40E0" // ClaimUid
        //+ "05" // AdditionalData length header
        //+ "E282AC3437"; // AdditionalData ("€47") as UTF8
        var token = new AntiforgeryToken()
        {
            SecurityToken = _securityToken,
            IsCookieToken = false,
            ClaimUid = _claimUid,
            AdditionalData = "€47"
        };

        // Act
        var actualSerializedData = testSerializer.Serialize(token);
        var deserializedToken = testSerializer.Deserialize(actualSerializedData);

        // Assert
        AssertTokensEqual(token, deserializedToken);
        dataProtector.Verify();
    }

    [Theory]
    [InlineData(DataProtectorType.SpanDataProtector)]
    [InlineData(DataProtectorType.DefaultDataProtector)]
    public void Serialize_FieldToken_WithUsername_TokenRoundTripSuccessful(DataProtectorType protectorType)
    {
        // Arrange
        var dataProtector = CreateDataProtector(protectorType);
        var testSerializer = new DefaultAntiforgeryTokenSerializer(dataProtector.Object);

        //"01" // Version
        //+ "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        //+ "00" // IsCookieToken
        //+ "00" // IsClaimsBased
        //+ "08" // Username length header
        //+ "4AC3A972C3B46D65" // Username ("Jérôme") as UTF8
        //+ "05" // AdditionalData length header
        //+ "E282AC3437"; // AdditionalData ("€47") as UTF8
        var token = new AntiforgeryToken()
        {
            SecurityToken = _securityToken,
            IsCookieToken = false,
            Username = "Jérôme",
            AdditionalData = "€47"
        };

        // Act
        var actualSerializedData = testSerializer.Serialize(token);
        var deserializedToken = testSerializer.Deserialize(actualSerializedData);

        // Assert
        AssertTokensEqual(token, deserializedToken);
        dataProtector.Verify();
    }

    [Theory]
    [InlineData(DataProtectorType.SpanDataProtector)]
    [InlineData(DataProtectorType.DefaultDataProtector)]
    public void Serialize_CookieToken_TokenRoundTripSuccessful(DataProtectorType protectorType)
    {
        // Arrange
        var dataProtector = CreateDataProtector(protectorType);
        var testSerializer = new DefaultAntiforgeryTokenSerializer(dataProtector.Object);

        //"01" // Version
        //+ "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        //+ "01"; // IsCookieToken
        var token = new AntiforgeryToken()
        {
            SecurityToken = _securityToken,
            IsCookieToken = true
        };

        // Act
        var actualSerializedData = testSerializer.Serialize(token);
        var deserializedToken = testSerializer.Deserialize(actualSerializedData);

        // Assert
        AssertTokensEqual(token, deserializedToken);
        dataProtector.Verify();
    }

    private static Mock<IDataProtectionProvider> CreateDataProtector(DataProtectorType protectorType, Action? onUnprotect = null)
    {
        IDataProtector dataProtector = protectorType switch
        {
            DataProtectorType.SpanDataProtector => new TestSpanDataProtector(onUnprotect),
            DataProtectorType.DefaultDataProtector => new TestDataProtector(onUnprotect),
            _ => throw new ArgumentOutOfRangeException(nameof(protectorType))
        };

        var provider = new Mock<IDataProtectionProvider>();
        provider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(dataProtector);
        return provider;
    }

    private static void AssertTokensEqual(AntiforgeryToken expected, AntiforgeryToken actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.AdditionalData, actual.AdditionalData);
        Assert.Equal(expected.ClaimUid, actual.ClaimUid);
        Assert.Equal(expected.IsCookieToken, actual.IsCookieToken);
        Assert.Equal(expected.SecurityToken, actual.SecurityToken);
        Assert.Equal(expected.Username, actual.Username);
    }

    /// <summary>
    /// A test data protector that implements <see cref="ISpanDataProtector"/>
    /// to exercise the optimized code path.
    /// </summary>
    private sealed class TestSpanDataProtector(Action? onUnprotect) : ISpanDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;

        public void Protect<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination) where TWriter : IBufferWriter<byte>, allows ref struct
        {
            var result = ProtectImpl(plaintext.ToArray());
            var destinationSpan = destination.GetSpan(result.Length);
            result.CopyTo(destinationSpan);
            destination.Advance(result.Length);
        }

        public void Unprotect<TWriter>(ReadOnlySpan<byte> protectedData, ref TWriter destination)
            where TWriter : IBufferWriter<byte>, allows ref struct
        {
            onUnprotect?.Invoke();
            var result = UnprotectImpl(protectedData.ToArray());
            var destinationSpan = destination.GetSpan(result.Length);
            result.CopyTo(destinationSpan);
            destination.Advance(result.Length);
        }

        public byte[] Protect(byte[] plaintext) => ProtectImpl(plaintext);

        public byte[] Unprotect(byte[] protectedData)
        {
            onUnprotect?.Invoke();
            return UnprotectImpl(protectedData);
        }

        private static byte[] ProtectImpl(byte[] data)
        {
            var input = new List<byte>(data);
            input.Add(_salt);
            return input.ToArray();
        }

        private static byte[] UnprotectImpl(byte[] data)
        {
            var salt = data[data.Length - 1];
            if (salt != _salt)
            {
                throw new ArgumentException("Invalid salt value in data");
            }
            return data.Take(data.Length - 1).ToArray();
        }
    }

    /// <summary>
    /// A test data protector that only implements <see cref="IDataProtector"/>
    /// (not <see cref="ISpanDataProtector"/>) to exercise the fallback code path.
    /// </summary>
    private sealed class TestDataProtector(Action? onUnprotect) : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;

        public byte[] Protect(byte[] plaintext)
        {
            var input = new List<byte>(plaintext);
            input.Add(_salt);
            return input.ToArray();
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            onUnprotect?.Invoke();
            var salt = protectedData[protectedData.Length - 1];
            if (salt != _salt)
            {
                throw new ArgumentException("Invalid salt value in data");
            }
            return protectedData.Take(protectedData.Length - 1).ToArray();
        }
    }
}
