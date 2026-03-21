// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// Represents a configured authenticated encryption mechanism which uses
/// managed <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> and
/// <see cref="System.Security.Cryptography.KeyedHashAlgorithm"/> types.
/// </summary>
public sealed class ManagedAuthenticatedEncryptorConfiguration : AlgorithmConfiguration, IInternalAlgorithmConfiguration
{
    /// <summary>
    /// The type of the algorithm to use for symmetric encryption.
    /// The type must subclass <see cref="SymmetricAlgorithm"/>.
    /// This property is required to have a value.
    /// </summary>
    /// <remarks>
    /// The algorithm must support CBC-style encryption and PKCS#7 padding and must have a block size of 64 bits or greater.
    /// The default algorithm is AES.
    /// </remarks>
    [ApplyPolicy]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type EncryptionAlgorithmType { get; set; } = typeof(Aes);

    /// <summary>
    /// The length (in bits) of the key that will be used for symmetric encryption.
    /// This property is required to have a value.
    /// </summary>
    /// <remarks>
    /// The key length must be 128 bits or greater.
    /// The default value is 256.
    /// </remarks>
    [ApplyPolicy]
    public int EncryptionAlgorithmKeySize { get; set; } = 256;

    /// <summary>
    /// The type of the algorithm to use for validation.
    /// Type type must subclass <see cref="KeyedHashAlgorithm"/>.
    /// This property is required to have a value.
    /// </summary>
    /// <remarks>
    /// The algorithm must have a digest length of 128 bits or greater.
    /// The default algorithm is HMACSHA256.
    /// </remarks>
    [ApplyPolicy]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type ValidationAlgorithmType { get; set; } = typeof(HMACSHA256);

    /// <inheritdoc />
    public override IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
    {
        var internalConfiguration = (IInternalAlgorithmConfiguration)this;
        return internalConfiguration.CreateDescriptorFromSecret(Secret.Random(KDK_SIZE_IN_BYTES));
    }

    IAuthenticatedEncryptorDescriptor IInternalAlgorithmConfiguration.CreateDescriptorFromSecret(ISecret secret)
    {
        return new ManagedAuthenticatedEncryptorDescriptor(this, secret);
    }

    /// <summary>
    /// Validates that this <see cref="ManagedAuthenticatedEncryptorConfiguration"/> is well-formed, i.e.,
    /// that the specified algorithms actually exist and can be instantiated properly.
    /// An exception will be thrown if validation fails.
    /// </summary>
    void IInternalAlgorithmConfiguration.Validate()
    {
        var factory = new ManagedAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);
        // Run a sample payload through an encrypt -> decrypt operation to make sure data round-trips properly.
        using var secret = Secret.Random(512 / 8);
        using var encryptor = factory.CreateAuthenticatedEncryptorInstance(secret, this);
        encryptor.PerformSelfTest();
    }
}
