// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

public class KeyRingTests
{
    [Fact]
    public void DefaultAuthenticatedEncryptor_Prop_InstantiationIsDeferred()
    {
        // Arrange
        var expectedEncryptorInstance = new Mock<IAuthenticatedEncryptor>().Object;

        var key1 = new MyKey(expectedEncryptorInstance: expectedEncryptorInstance);
        var key2 = new MyKey();

        // Act
        var keyRing = new KeyRing(key1, new[] { key1, key2 });

        // Assert
        Assert.Equal(0, key1.NumTimesCreateEncryptorInstanceCalled);
        Assert.Same(expectedEncryptorInstance, keyRing.DefaultAuthenticatedEncryptor);
        Assert.Equal(1, key1.NumTimesCreateEncryptorInstanceCalled);
        Assert.Same(expectedEncryptorInstance, keyRing.DefaultAuthenticatedEncryptor);
        Assert.Equal(1, key1.NumTimesCreateEncryptorInstanceCalled); // should've been cached
    }

    [Fact]
    public void DefaultKeyId_Prop()
    {
        // Arrange
        var key1 = new MyKey();
        var key2 = new MyKey();

        // Act
        var keyRing = new KeyRing(key2, new[] { key1, key2 });

        // Assert
        Assert.Equal(key2.KeyId, keyRing.DefaultKeyId);
    }

    [Fact]
    public void DefaultKeyIdAndEncryptor_IfDefaultKeyNotPresentInAllKeys()
    {
        // Arrange
        var key1 = new MyKey();
        var key2 = new MyKey();
        var key3 = new MyKey(expectedEncryptorInstance: new Mock<IAuthenticatedEncryptor>().Object);

        // Act
        var keyRing = new KeyRing(key3, new[] { key1, key2 });

        // Assert
        Assert.Equal(key3.KeyId, keyRing.DefaultKeyId);
        Assert.Equal(key3.CreateEncryptor(), keyRing.GetAuthenticatedEncryptorByKeyId(key3.KeyId, out var _));
    }

    [Fact]
    public void GetAuthenticatedEncryptorByKeyId_DefersInstantiation_AndReturnsRevocationInfo()
    {
        // Arrange
        var expectedEncryptorInstance1 = new Mock<IAuthenticatedEncryptor>().Object;
        var expectedEncryptorInstance2 = new Mock<IAuthenticatedEncryptor>().Object;

        var key1 = new MyKey(expectedEncryptorInstance: expectedEncryptorInstance1, isRevoked: true);
        var key2 = new MyKey(expectedEncryptorInstance: expectedEncryptorInstance2);

        // Act
        var keyRing = new KeyRing(key2, new[] { key1, key2 });

        // Assert
        Assert.Equal(0, key1.NumTimesCreateEncryptorInstanceCalled);
        Assert.Same(expectedEncryptorInstance1, keyRing.GetAuthenticatedEncryptorByKeyId(key1.KeyId, out var isRevoked));
        Assert.True(isRevoked);
        Assert.Equal(1, key1.NumTimesCreateEncryptorInstanceCalled);
        Assert.Same(expectedEncryptorInstance1, keyRing.GetAuthenticatedEncryptorByKeyId(key1.KeyId, out isRevoked));
        Assert.True(isRevoked);
        Assert.Equal(1, key1.NumTimesCreateEncryptorInstanceCalled);
        Assert.Equal(0, key2.NumTimesCreateEncryptorInstanceCalled);
        Assert.Same(expectedEncryptorInstance2, keyRing.GetAuthenticatedEncryptorByKeyId(key2.KeyId, out isRevoked));
        Assert.False(isRevoked);
        Assert.Equal(1, key2.NumTimesCreateEncryptorInstanceCalled);
        Assert.Same(expectedEncryptorInstance2, keyRing.GetAuthenticatedEncryptorByKeyId(key2.KeyId, out isRevoked));
        Assert.False(isRevoked);
        Assert.Equal(1, key2.NumTimesCreateEncryptorInstanceCalled);
        Assert.Same(expectedEncryptorInstance2, keyRing.DefaultAuthenticatedEncryptor);
        Assert.Equal(1, key2.NumTimesCreateEncryptorInstanceCalled);
    }

    private sealed class MyKey : IKey
    {
        public int NumTimesCreateEncryptorInstanceCalled;
        private readonly Func<IAuthenticatedEncryptor> _encryptorFactory;

        public MyKey(bool isRevoked = false, IAuthenticatedEncryptor expectedEncryptorInstance = null)
        {
            CreationDate = DateTimeOffset.Now;
            ActivationDate = CreationDate + TimeSpan.FromHours(1);
            ExpirationDate = CreationDate + TimeSpan.FromDays(30);
            IsRevoked = isRevoked;
            KeyId = Guid.NewGuid();
            _encryptorFactory = () => expectedEncryptorInstance ?? new Mock<IAuthenticatedEncryptor>().Object;
        }

        public DateTimeOffset ActivationDate { get; }
        public DateTimeOffset CreationDate { get; }
        public DateTimeOffset ExpirationDate { get; }
        public bool IsRevoked { get; }
        public Guid KeyId { get; }
        public IAuthenticatedEncryptorDescriptor Descriptor => throw new NotImplementedException();

        public IAuthenticatedEncryptor CreateEncryptor()
        {
            NumTimesCreateEncryptorInstanceCalled++;
            return _encryptorFactory();
        }
    }
}
