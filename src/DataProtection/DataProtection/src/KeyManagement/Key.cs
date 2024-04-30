// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// The basic implementation of <see cref="IKey"/>.
/// </summary>
internal sealed class Key : IKey
{
    private IAuthenticatedEncryptorDescriptor? _descriptor;

    // If descriptor is available at construction time, these will remain null forever
    private readonly object? _descriptorLock; // Protects _descriptor and _descriptorException
    private readonly Func<IAuthenticatedEncryptorDescriptor>? _descriptorFactory; // May not be used
    private Exception? _descriptorException;

    private readonly IEnumerable<IAuthenticatedEncryptorFactory> _encryptorFactories;

    private IAuthenticatedEncryptor? _encryptor;

    /// <summary>
    /// The basic implementation of <see cref="IKey"/>, where the <see cref="IAuthenticatedEncryptorDescriptor"/>
    /// has already been created.
    /// </summary>
    public Key(
        Guid keyId,
        DateTimeOffset creationDate,
        DateTimeOffset activationDate,
        DateTimeOffset expirationDate,
        IAuthenticatedEncryptorDescriptor descriptor,
        IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories)
        : this(keyId,
              creationDate,
              activationDate,
              expirationDate,
              encryptorFactories,
              descriptor,
              descriptorFactory: null,
              descriptorException: null)
    {
    }

    /// <summary>
    /// The basic implementation of <see cref="IKey"/>, where the incoming XML element
    /// hasn't yet been fully processed.
    /// </summary>
    public Key(
        Guid keyId,
        DateTimeOffset creationDate,
        DateTimeOffset activationDate,
        DateTimeOffset expirationDate,
        IInternalXmlKeyManager keyManager,
        XElement keyElement,
        IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories)
        : this(keyId,
              creationDate,
              activationDate,
              expirationDate,
              encryptorFactories,
              descriptor: null,
              descriptorFactory: GetLazyDescriptorDelegate(keyManager, keyElement),
              descriptorException: null)
    {
    }

    // internal for testing
    internal Key(
        Guid keyId,
        DateTimeOffset creationDate,
        DateTimeOffset activationDate,
        DateTimeOffset expirationDate,
        IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories,
        Func<IAuthenticatedEncryptorDescriptor>? descriptorFactory)
        : this(keyId,
              creationDate,
              activationDate,
              expirationDate,
              encryptorFactories,
              descriptor: null,
              descriptorFactory: descriptorFactory,
              descriptorException: null)
    {
    }

    private Key(
        Guid keyId,
        DateTimeOffset creationDate,
        DateTimeOffset activationDate,
        DateTimeOffset expirationDate,
        IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories,
        IAuthenticatedEncryptorDescriptor? descriptor,
        Func<IAuthenticatedEncryptorDescriptor>? descriptorFactory,
        Exception? descriptorException)
    {
        KeyId = keyId;
        CreationDate = creationDate;
        ActivationDate = activationDate;
        ExpirationDate = expirationDate;
        _encryptorFactories = encryptorFactories;
        _descriptor = descriptor;
        _descriptorFactory = descriptorFactory;
        _descriptorException = descriptorException;
        _descriptorLock = descriptor is null ? new() : null;
    }

    public DateTimeOffset ActivationDate { get; }

    public DateTimeOffset CreationDate { get; }

    public DateTimeOffset ExpirationDate { get; }

    public bool IsRevoked { get; private set; }

    public Guid KeyId { get; }

    public IAuthenticatedEncryptorDescriptor Descriptor
    {
        get
        {
            // We could check for _descriptorException here, but there's no reason to optimize that case
            // (i.e. by avoiding taking the lock)

            if (_descriptor is not null) // Can only go from null to non-null, so losing a race here doesn't matter
            {
                Debug.Assert(_descriptorException is null); // Mutually exclusive with _descriptor
                return _descriptor;
            }

            lock (_descriptorLock!)
            {
                if (_descriptorException is not null)
                {
                    throw _descriptorException;
                }

                if (_descriptor is not null)
                {
                    return _descriptor;
                }

                Debug.Assert(_descriptorFactory is not null, "Key constructed without either descriptor or descriptor factory");

                try
                {
                    _descriptor = _descriptorFactory();
                    return _descriptor;
                }
                catch (Exception ex)
                {
                    _descriptorException = ex;
                    throw;
                }
            }
        }
    }

    internal void ResetDescriptor()
    {
        if (_descriptor is not null)
        {
            Debug.Fail("ResetDescriptor called with descriptor available");
            Debug.Assert(_descriptorException is null); // Mutually exclusive with _descriptor
            return;
        }

        lock (_descriptorLock!)
        {
            _descriptor = null;
            _descriptorException = null;
        }
    }

    public IAuthenticatedEncryptor? CreateEncryptor()
    {
        if (_encryptor == null)
        {
            foreach (var factory in _encryptorFactories)
            {
                var encryptor = factory.CreateEncryptorInstance(this);
                if (encryptor != null)
                {
                    _encryptor = encryptor;
                    break;
                }
            }
        }

        return _encryptor;
    }

    internal void SetRevoked()
    {
        IsRevoked = true;
    }

    internal Key Clone()
    {
        // Note that we don't reuse _descriptorLock
        return new Key(
            keyId: KeyId,
            creationDate: CreationDate,
            activationDate: ActivationDate,
            expirationDate: ExpirationDate,
            encryptorFactories: _encryptorFactories,
            descriptor: _descriptor,
            descriptorFactory: _descriptorFactory,
            descriptorException: _descriptorException)
        {
            IsRevoked = IsRevoked,
        };
    }

    private static Func<IAuthenticatedEncryptorDescriptor> GetLazyDescriptorDelegate(IInternalXmlKeyManager keyManager, XElement keyElement)
    {
        // The <key> element will be held around in memory for a potentially lengthy period
        // of time. Since it might contain sensitive information, we should protect it.
        var encryptedKeyElement = keyElement.ToSecret();

        try
        {
            return GetLazyDescriptorDelegate;
        }
        finally
        {
            // It's important that the lambda above doesn't capture 'descriptorElement'. Clearing the reference here
            // helps us detect if we've done this by causing a null ref at runtime.
            keyElement = null!;
        }

        IAuthenticatedEncryptorDescriptor GetLazyDescriptorDelegate()
        {
            return keyManager.DeserializeDescriptorFromKeyElement(encryptedKeyElement.ToXElement());
        }
    }
}
