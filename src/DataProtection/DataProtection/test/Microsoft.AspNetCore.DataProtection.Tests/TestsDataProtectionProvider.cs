// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.Tests;

// use T = CngGcmAuthenticatedEncryptorConfiguration; for fastest implementation: AES-256-GCM [CNG]
// use T = ManagedAuthenticatedEncryptorConfiguration; for slowest implementation: AES-256-CBC + HMACSHA256 [Managed]
internal class TestsDataProtectionProvider<T> : IDataProtectionProvider
    where T : AlgorithmConfiguration, new()
{
    private readonly KeyRingBasedDataProtectionProvider _dataProtectionProvider;

    public TestsDataProtectionProvider() : this(NullLoggerFactory.Instance)
    {
    }

    public TestsDataProtectionProvider(ILoggerFactory loggerFactory)
    {
        ArgumentNullThrowHelper.ThrowIfNull(loggerFactory);

        IKeyRingProvider keyringProvider = new EphemeralKeyRing(loggerFactory);
        var logger = loggerFactory.CreateLogger<EphemeralDataProtectionProvider>();
        logger.UsingEphemeralDataProtectionProvider();

        _dataProtectionProvider = new KeyRingBasedDataProtectionProvider(keyringProvider, loggerFactory);
    }

    /// <inheritdoc />
    public IDataProtector CreateProtector(string purpose)
    {
        ArgumentNullThrowHelper.ThrowIfNull(purpose);

        // just forward to the underlying provider
        return _dataProtectionProvider.CreateProtector(purpose);
    }

    private sealed class EphemeralKeyRing : IKeyRing, IKeyRingProvider
    {
        public EphemeralKeyRing(ILoggerFactory loggerFactory)
        {
            DefaultAuthenticatedEncryptor = GetDefaultEncryptor(loggerFactory);
        }

        public IAuthenticatedEncryptor DefaultAuthenticatedEncryptor { get; }

        public Guid DefaultKeyId { get; }

        public IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(Guid keyId, out bool isRevoked)
        {
            isRevoked = false;
            return (keyId == default(Guid)) ? DefaultAuthenticatedEncryptor : null;
        }

        public IKeyRing GetCurrentKeyRing()
        {
            return this;
        }

        private static IAuthenticatedEncryptor GetDefaultEncryptor(ILoggerFactory loggerFactory)
        {
            var configuration = new T();
            if (configuration is CngGcmAuthenticatedEncryptorConfiguration cngConfiguration)
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

                var descriptor = (CngGcmAuthenticatedEncryptorDescriptor)new T().CreateNewDescriptor();
                return new CngGcmAuthenticatedEncryptorFactory(loggerFactory)
                    .CreateAuthenticatedEncryptorInstance(
                        descriptor.MasterKey,
                        cngConfiguration);
            }
            else if (configuration is ManagedAuthenticatedEncryptorConfiguration managedConfiguration)
            {
                var descriptor = (ManagedAuthenticatedEncryptorDescriptor)new T().CreateNewDescriptor();
                return new ManagedAuthenticatedEncryptorFactory(loggerFactory)
                    .CreateAuthenticatedEncryptorInstance(
                        descriptor.MasterKey,
                        managedConfiguration);
            }

            throw new NotSupportedException($"Such type of Encryptor is not supported: {typeof(T)}");
        }
    }
}
