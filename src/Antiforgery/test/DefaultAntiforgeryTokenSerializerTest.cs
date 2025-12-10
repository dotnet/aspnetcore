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
    private static readonly Mock<IDataProtectionProvider> _dataProtector = GetDataProtector();
    private static readonly BinaryBlob _claimUid = new BinaryBlob(256, new byte[] { 0x6F, 0x16, 0x48, 0xE9, 0x72, 0x49, 0xAA, 0x58, 0x75, 0x40, 0x36, 0xA6, 0x7E, 0x24, 0x8C, 0xF0, 0x44, 0xF0, 0x7E, 0xCF, 0xB0, 0xED, 0x38, 0x75, 0x56, 0xCE, 0x02, 0x9A, 0x4F, 0x9A, 0x40, 0xE0 });
    private static readonly BinaryBlob _securityToken = new BinaryBlob(128, new byte[] { 0x70, 0x5E, 0xED, 0xCC, 0x7D, 0x42, 0xF1, 0xD6, 0xB3, 0xB9, 0x8A, 0x59, 0x36, 0x25, 0xBB, 0x4C });
    private static readonly ObjectPool<AntiforgerySerializationContext> _pool =
        new DefaultObjectPoolProvider().Create(new AntiforgerySerializationContextPooledObjectPolicy());
    private const byte _salt = 0x05;

    [Theory]
    [InlineData(
        "01" // Version
        + "705EEDCC7D42F1D6B3B9" // SecurityToken
                                 // (WRONG!) Stream ends too early
        )]
    [InlineData(
        "01" // Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "01" // IsCookieToken
        + "00" // (WRONG!) Too much data in stream
        )]
    [InlineData(
        "02" // (WRONG! - must be 0x01) Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "01" // IsCookieToken
        )]
    [InlineData(
        "01" // Version
        + "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        + "00" // IsCookieToken
        + "00" // IsClaimsBased
        + "05" // Username length header
        + "0000" // (WRONG!) Too little data in stream
        )]
    public void Deserialize_BadToken_Throws(string serializedToken)
    {
        // Arrange
        var testSerializer = new DefaultAntiforgeryTokenSerializer(_dataProtector.Object, _pool);

        // Act & assert
        var ex = Assert.Throws<AntiforgeryValidationException>(() => testSerializer.Deserialize(serializedToken));
        Assert.Equal(@"The antiforgery token could not be decrypted.", ex.Message);
    }

    [Fact]
    public void Serialize_FieldToken_WithClaimUid_TokenRoundTripSuccessful()
    {
        // Arrange
        var testSerializer = new DefaultAntiforgeryTokenSerializer(_dataProtector.Object, _pool);

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
        _dataProtector.Verify();
    }

    [Fact]
    public void Serialize_FieldToken_WithUsername_TokenRoundTripSuccessful()
    {
        // Arrange
        var testSerializer = new DefaultAntiforgeryTokenSerializer(_dataProtector.Object, _pool);

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
        _dataProtector.Verify();
    }

    [Fact]
    public void Serialize_CookieToken_TokenRoundTripSuccessful()
    {
        // Arrange
        var testSerializer = new DefaultAntiforgeryTokenSerializer(_dataProtector.Object, _pool);

        //"01" // Version
        //+ "705EEDCC7D42F1D6B3B98A593625BB4C" // SecurityToken
        //+ "01"; // IsCookieToken
        var token = new AntiforgeryToken()
        {
            SecurityToken = _securityToken,
            IsCookieToken = true
        };

        // Act
        string actualSerializedData = testSerializer.Serialize(token);
        var deserializedToken = testSerializer.Deserialize(actualSerializedData);

        // Assert
        AssertTokensEqual(token, deserializedToken);
        _dataProtector.Verify();
    }

    private static Mock<IDataProtectionProvider> GetDataProtector()
    {
        var testSpanDataProtector = new TestSpanDataProtector();

        var provider = new Mock<IDataProtectionProvider>();
        provider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(testSpanDataProtector);
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

    private sealed class TestSpanDataProtector : ISpanDataProtector
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
            var result = UnprotectImpl(protectedData.ToArray());
            var destinationSpan = destination.GetSpan(result.Length);
            result.CopyTo(destinationSpan);
            destination.Advance(result.Length);
        }

        public byte[] Protect(byte[] plaintext) => ProtectImpl(plaintext);

        public byte[] Unprotect(byte[] protectedData) => UnprotectImpl(protectedData);

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
}
