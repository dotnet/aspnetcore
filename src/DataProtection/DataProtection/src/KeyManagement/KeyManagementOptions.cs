// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// Options that control how an <see cref="IKeyManager"/> should behave.
/// </summary>
public class KeyManagementOptions
{
    private static readonly TimeSpan _keyPropagationWindow = TimeSpan.FromDays(2);
    private static readonly TimeSpan _keyRingRefreshPeriod = TimeSpan.FromHours(24);
    private static readonly TimeSpan _maxServerClockSkew = TimeSpan.FromMinutes(5);
    private TimeSpan _newKeyLifetime = TimeSpan.FromDays(90);

    /// <summary>
    /// Initializes a new instance of <see cref="KeyManagementOptions"/>.
    /// </summary>
    public KeyManagementOptions()
    {
    }

    /// <summary>
    /// Specifies whether the data protection system should auto-generate keys.
    /// </summary>
    /// <remarks>
    /// If this value is 'false', the system will not generate new keys automatically.
    /// The key ring must contain at least one active non-revoked key, otherwise calls
    /// to <see cref="IDataProtector.Protect(byte[])"/> may fail. The system may end up
    /// protecting payloads to expired keys if this property is set to 'false'.
    /// The default value is 'true'.
    /// </remarks>
    public bool AutoGenerateKeys { get; set; } = true;

    /// <summary>
    /// Specifies the period before key expiration in which a new key should be generated
    /// so that it has time to propagate fully throughout the key ring. For example, if this
    /// period is 72 hours, then a new key will be created and persisted to storage
    /// approximately 72 hours before expiration.
    /// </summary>
    /// <remarks>
    /// This value is currently fixed at 48 hours.
    /// </remarks>
    internal static TimeSpan KeyPropagationWindow
    {
        get
        {
            // This value is not settable since there's a complex interaction between
            // it and the key ring refresh period.
            return _keyPropagationWindow;
        }
    }

    /// <summary>
    /// Controls the auto-refresh period where the key ring provider will
    /// flush its collection of cached keys and reread the collection from
    /// backing storage.
    /// </summary>
    /// <remarks>
    /// This value is currently fixed at 24 hours.
    /// </remarks>
    internal static TimeSpan KeyRingRefreshPeriod
    {
        get
        {
            // This value is not settable since there's a complex interaction between
            // it and the key expiration safety period.
            return _keyRingRefreshPeriod;
        }
    }

    /// <summary>
    /// Specifies the maximum clock skew allowed between servers when reading
    /// keys from the key ring. The key ring may use a key which has not yet
    /// been activated or which has expired if the key's valid lifetime is within
    /// the allowed clock skew window. This value can be set to <see cref="TimeSpan.Zero"/>
    /// if key activation and expiration times should be strictly honored by this server.
    /// </summary>
    /// <remarks>
    /// This value is currently fixed at 5 minutes.
    /// </remarks>
    internal static TimeSpan MaxServerClockSkew
    {
        get
        {
            return _maxServerClockSkew;
        }
    }

    /// <summary>
    /// During each default key resolution, if a key decryption attempt fails,
    /// it can be retried, as long as the total number of retries across all keys
    /// does not exceed this value.
    /// </summary>
    /// <remarks>
    /// Settable for testing.
    /// </remarks>
    internal int MaximumTotalDefaultKeyResolverRetries { get; set; } = 10;

    /// <summary>
    /// Wait this long before each default key resolution decryption retry.
    /// <seealso cref="MaximumTotalDefaultKeyResolverRetries"/>
    /// </summary>
    /// <remarks>
    /// Settable for testing.
    /// </remarks>
    internal TimeSpan DefaultKeyResolverRetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Controls the lifetime (number of days before expiration)
    /// for newly-generated keys.
    /// </summary>
    /// <remarks>
    /// The lifetime cannot be less than one week.
    /// The default value is 90 days.
    /// </remarks>
    public TimeSpan NewKeyLifetime
    {
        get
        {
            return _newKeyLifetime;
        }
        set
        {
            if (value < TimeSpan.FromDays(7))
            {
                throw new ArgumentOutOfRangeException(nameof(value), Resources.KeyManagementOptions_MinNewKeyLifetimeViolated);
            }
            _newKeyLifetime = value;
        }
    }

    /// <summary>
    /// The <see cref="AlgorithmConfiguration"/> instance that can be used to create
    /// the <see cref="IAuthenticatedEncryptorDescriptor"/> instance.
    /// </summary>
    public AlgorithmConfiguration? AuthenticatedEncryptorConfiguration { get; set; }

    /// <summary>
    /// The list of <see cref="IKeyEscrowSink"/> to store the key material in.
    /// </summary>
    public IList<IKeyEscrowSink> KeyEscrowSinks { get; } = new List<IKeyEscrowSink>();

    /// <summary>
    /// The <see cref="IXmlRepository"/> to use for storing and retrieving XML elements.
    /// </summary>
    public IXmlRepository? XmlRepository { get; set; }

    /// <summary>
    /// The <see cref="IXmlEncryptor"/> to use for encrypting XML elements.
    /// </summary>
    public IXmlEncryptor? XmlEncryptor { get; set; }

    /// <summary>
    /// The list of <see cref="IAuthenticatedEncryptorFactory"/> that will be used for creating
    /// <see cref="IAuthenticatedEncryptor"/>s.
    /// </summary>
    public IList<IAuthenticatedEncryptorFactory> AuthenticatedEncryptorFactories { get; } = new List<IAuthenticatedEncryptorFactory>();
}
