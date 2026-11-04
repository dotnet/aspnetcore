// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

/// <summary>
/// An <see cref="IAuthenticatedEncryptorFactory"/> for <see cref="CbcAuthenticatedEncryptor"/>.
/// </summary>
public sealed class CngCbcAuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CngCbcAuthenticatedEncryptorFactory"/>.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public CngCbcAuthenticatedEncryptorFactory(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CngCbcAuthenticatedEncryptorFactory>();
    }

    /// <inheritdoc />
    public IAuthenticatedEncryptor? CreateEncryptorInstance(IKey key)
    {
        if (key.Descriptor is not CngCbcAuthenticatedEncryptorDescriptor descriptor)
        {
            return null;
        }

        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        return CreateAuthenticatedEncryptorInstance(descriptor.MasterKey, descriptor.Configuration);
    }

    [SupportedOSPlatform("windows")]
    [return: NotNullIfNotNull("configuration")]
    internal CbcAuthenticatedEncryptor? CreateAuthenticatedEncryptorInstance(
        ISecret secret,
        CngCbcAuthenticatedEncryptorConfiguration? configuration)
    {
        if (configuration == null)
        {
            return null;
        }

        using var key = new Secret(secret);
        return new CbcAuthenticatedEncryptor(
            keyDerivationKey: key,
            symmetricAlgorithmHandle: GetSymmetricBlockCipherAlgorithmHandle(configuration),
            symmetricAlgorithmKeySizeInBytes: (uint)(configuration.EncryptionAlgorithmKeySize / 8),
            hmacAlgorithmHandle: GetHmacAlgorithmHandle(configuration));
    }

    [SupportedOSPlatform("windows")]
    private BCryptAlgorithmHandle GetHmacAlgorithmHandle(CngCbcAuthenticatedEncryptorConfiguration configuration)
    {
        // basic argument checking
        if (String.IsNullOrEmpty(configuration.HashAlgorithm))
        {
            throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(configuration.HashAlgorithm));
        }

        _logger.OpeningCNGAlgorithmFromProviderWithHMAC(configuration.HashAlgorithm, configuration.HashAlgorithmProvider);
        BCryptAlgorithmHandle? algorithmHandle = null;

        // Special-case cached providers
        if (configuration.HashAlgorithmProvider == null)
        {
            if (configuration.HashAlgorithm == Constants.BCRYPT_SHA1_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.HMAC_SHA1; }
            else if (configuration.HashAlgorithm == Constants.BCRYPT_SHA256_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.HMAC_SHA256; }
            else if (configuration.HashAlgorithm == Constants.BCRYPT_SHA512_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.HMAC_SHA512; }
        }

        // Look up the provider dynamically if we couldn't fetch a cached instance
        if (algorithmHandle == null)
        {
            algorithmHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(configuration.HashAlgorithm, configuration.HashAlgorithmProvider, hmac: true);
        }

        // Make sure we're using a hash algorithm. We require a minimum 128-bit digest.
        uint digestSize = algorithmHandle.GetHashDigestLength();
        AlgorithmAssert.IsAllowableValidationAlgorithmDigestSize(checked(digestSize * 8));

        // all good!
        return algorithmHandle;
    }

    [SupportedOSPlatform("windows")]
    private BCryptAlgorithmHandle GetSymmetricBlockCipherAlgorithmHandle(CngCbcAuthenticatedEncryptorConfiguration configuration)
    {
        // basic argument checking
        if (String.IsNullOrEmpty(configuration.EncryptionAlgorithm))
        {
            throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(EncryptionAlgorithm));
        }
        if (configuration.EncryptionAlgorithmKeySize < 0)
        {
            throw Error.Common_PropertyMustBeNonNegative(nameof(configuration.EncryptionAlgorithmKeySize));
        }

        _logger.OpeningCNGAlgorithmFromProviderWithChainingModeCBC(configuration.EncryptionAlgorithm, configuration.EncryptionAlgorithmProvider);

        BCryptAlgorithmHandle? algorithmHandle = null;

        // Special-case cached providers
        if (configuration.EncryptionAlgorithmProvider == null)
        {
            if (configuration.EncryptionAlgorithm == Constants.BCRYPT_AES_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.AES_CBC; }
        }

        // Look up the provider dynamically if we couldn't fetch a cached instance
        if (algorithmHandle == null)
        {
            algorithmHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(configuration.EncryptionAlgorithm, configuration.EncryptionAlgorithmProvider);
            algorithmHandle.SetChainingMode(Constants.BCRYPT_CHAIN_MODE_CBC);
        }

        // make sure we're using a block cipher with an appropriate key size & block size
        AlgorithmAssert.IsAllowableSymmetricAlgorithmBlockSize(checked(algorithmHandle.GetCipherBlockLength() * 8));
        AlgorithmAssert.IsAllowableSymmetricAlgorithmKeySize(checked((uint)configuration.EncryptionAlgorithmKeySize));

        // make sure the provided key length is valid
        algorithmHandle.GetSupportedKeyLengths().EnsureValidKeyLength((uint)configuration.EncryptionAlgorithmKeySize);

        // all good!
        return algorithmHandle;
    }
}
