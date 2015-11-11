// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNet.DataProtection.Internal;
using Microsoft.AspNet.DataProtection.KeyManagement.Internal;
using Microsoft.AspNet.DataProtection.Repositories;
using Microsoft.AspNet.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using static System.FormattableString;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    /// <summary>
    /// A key manager backed by an <see cref="IXmlRepository"/>.
    /// </summary>
    public sealed class XmlKeyManager : IKeyManager, IInternalXmlKeyManager
    {
        // Used for serializing elements to persistent storage
        internal static readonly XName KeyElementName = "key";
        internal static readonly XName IdAttributeName = "id";
        internal static readonly XName VersionAttributeName = "version";
        internal static readonly XName CreationDateElementName = "creationDate";
        internal static readonly XName ActivationDateElementName = "activationDate";
        internal static readonly XName ExpirationDateElementName = "expirationDate";
        internal static readonly XName DescriptorElementName = "descriptor";
        internal static readonly XName DeserializerTypeAttributeName = "deserializerType";
        internal static readonly XName RevocationElementName = "revocation";
        internal static readonly XName RevocationDateElementName = "revocationDate";
        internal static readonly XName ReasonElementName = "reason";

        private const string RevokeAllKeysValue = "*";

        private readonly IActivator _activator;
        private readonly IAuthenticatedEncryptorConfiguration _authenticatedEncryptorConfiguration;
        private readonly IInternalXmlKeyManager _internalKeyManager;
        private readonly IKeyEscrowSink _keyEscrowSink;
        private readonly ILogger _logger;

        private CancellationTokenSource _cacheExpirationTokenSource;

        /// <summary>
        /// Creates an <see cref="XmlKeyManager"/>.
        /// </summary>
        /// <param name="repository">The repository where keys are stored.</param>
        /// <param name="configuration">Configuration for newly-created keys.</param>
        /// <param name="services">A provider of optional services.</param>
        public XmlKeyManager(
            IXmlRepository repository,
            IAuthenticatedEncryptorConfiguration configuration,
            IServiceProvider services)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            KeyEncryptor = services.GetService<IXmlEncryptor>(); // optional
            KeyRepository = repository;

            _activator = services.GetActivator(); // returns non-null
            _authenticatedEncryptorConfiguration = configuration;
            _internalKeyManager = services.GetService<IInternalXmlKeyManager>() ?? this;
            _keyEscrowSink = services.GetKeyEscrowSink(); // not required
            _logger = services.GetLogger<XmlKeyManager>(); // not required
            TriggerAndResetCacheExpirationToken(suppressLogging: true);
        }

        internal XmlKeyManager(IServiceProvider services)
        {
            // First, see if an explicit encryptor or repository was specified.
            // If either was specified, then we won't use the fallback.
            KeyEncryptor = services.GetService<IXmlEncryptor>(); // optional
            KeyRepository = (KeyEncryptor != null)
                ? services.GetRequiredService<IXmlRepository>() // required if encryptor is specified
                : services.GetService<IXmlRepository>(); // optional if encryptor not specified

            // If the repository is missing, then we get both the encryptor and the repository from the fallback.
            // If the fallback is missing, the final call to GetRequiredService below will throw.
            if (KeyRepository == null)
            {
                var defaultKeyServices = services.GetService<IDefaultKeyServices>();
                KeyEncryptor = defaultKeyServices?.GetKeyEncryptor(); // optional
                KeyRepository = defaultKeyServices?.GetKeyRepository() ?? services.GetRequiredService<IXmlRepository>();
            }

            _activator = services.GetActivator(); // returns non-null
            _authenticatedEncryptorConfiguration = services.GetRequiredService<IAuthenticatedEncryptorConfiguration>();
            _internalKeyManager = services.GetService<IInternalXmlKeyManager>() ?? this;
            _keyEscrowSink = services.GetKeyEscrowSink(); // not required
            _logger = services.GetLogger<XmlKeyManager>(); // not required
            TriggerAndResetCacheExpirationToken(suppressLogging: true);
        }

        internal IXmlEncryptor KeyEncryptor { get; }

        internal IXmlRepository KeyRepository { get; }

        public IKey CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate)
        {
            return _internalKeyManager.CreateNewKey(
                keyId: Guid.NewGuid(),
                creationDate: DateTimeOffset.UtcNow,
                activationDate: activationDate,
                expirationDate: expirationDate);
        }

        private static string DateTimeOffsetToFilenameSafeString(DateTimeOffset dateTime)
        {
            // similar to the XML format for dates, but with punctuation stripped
            return dateTime.UtcDateTime.ToString("yyyyMMddTHHmmssFFFFFFFZ");
        }

        public IReadOnlyCollection<IKey> GetAllKeys()
        {
            var allElements = KeyRepository.GetAllElements();

            // We aggregate all the information we read into three buckets
            Dictionary<Guid, KeyBase> keyIdToKeyMap = new Dictionary<Guid, KeyBase>();
            HashSet<Guid> revokedKeyIds = null;
            DateTimeOffset? mostRecentMassRevocationDate = null;

            foreach (var element in allElements)
            {
                if (element.Name == KeyElementName)
                {
                    // ProcessKeyElement can return null in the case of failure, and if this happens we'll move on.
                    // Still need to throw if we see duplicate keys with the same id.
                    KeyBase key = ProcessKeyElement(element);
                    if (key != null)
                    {
                        if (keyIdToKeyMap.ContainsKey(key.KeyId))
                        {
                            throw Error.XmlKeyManager_DuplicateKey(key.KeyId);
                        }
                        keyIdToKeyMap[key.KeyId] = key;
                    }
                }
                else if (element.Name == RevocationElementName)
                {
                    object revocationInfo = ProcessRevocationElement(element);
                    if (revocationInfo is Guid)
                    {
                        // a single key was revoked
                        if (revokedKeyIds == null)
                        {
                            revokedKeyIds = new HashSet<Guid>();
                        }
                        revokedKeyIds.Add((Guid)revocationInfo);
                    }
                    else
                    {
                        // all keys as of a certain date were revoked
                        DateTimeOffset thisMassRevocationDate = (DateTimeOffset)revocationInfo;
                        if (!mostRecentMassRevocationDate.HasValue || mostRecentMassRevocationDate < thisMassRevocationDate)
                        {
                            mostRecentMassRevocationDate = thisMassRevocationDate;
                        }
                    }
                }
                else
                {
                    // Skip unknown elements.
                    _logger?.UnknownElementWithNameFoundInKeyringSkipping(element.Name);
                }
            }

            // Apply individual revocations
            if (revokedKeyIds != null)
            {
                foreach (Guid revokedKeyId in revokedKeyIds)
                {
                    KeyBase key;
                    keyIdToKeyMap.TryGetValue(revokedKeyId, out key);
                    if (key != null)
                    {
                        key.SetRevoked();
                        _logger?.MarkedKeyAsRevokedInTheKeyring(revokedKeyId);
                    }
                    else
                    {
                        _logger?.TriedToProcessRevocationOfKeyButNoSuchKeyWasFound(revokedKeyId);
                    }
                }
            }

            // Apply mass revocations
            if (mostRecentMassRevocationDate.HasValue)
            {
                foreach (var key in keyIdToKeyMap.Values)
                {
                    // The contract of IKeyManager.RevokeAllKeys is that keys created *strictly before* the
                    // revocation date are revoked. The system clock isn't very granular, and if this were
                    // a less-than-or-equal check we could end up with the weird case where a revocation
                    // immediately followed by a key creation results in a newly-created revoked key (since
                    // the clock hasn't yet stepped).
                    if (key.CreationDate < mostRecentMassRevocationDate)
                    {
                        key.SetRevoked();
                        _logger?.MarkedKeyAsRevokedInTheKeyring(key.KeyId);
                    }
                }
            }

            // And we're finished!
            return keyIdToKeyMap.Values.ToList().AsReadOnly();
        }

        public CancellationToken GetCacheExpirationToken()
        {
            return Interlocked.CompareExchange(ref _cacheExpirationTokenSource, null, null).Token;
        }

        private KeyBase ProcessKeyElement(XElement keyElement)
        {
            Debug.Assert(keyElement.Name == KeyElementName);

            try
            {
                // Read metadata and prepare the key for deferred instantiation
                Guid keyId = (Guid)keyElement.Attribute(IdAttributeName);
                DateTimeOffset creationDate = (DateTimeOffset)keyElement.Element(CreationDateElementName);
                DateTimeOffset activationDate = (DateTimeOffset)keyElement.Element(ActivationDateElementName);
                DateTimeOffset expirationDate = (DateTimeOffset)keyElement.Element(ExpirationDateElementName);

                _logger?.FoundKey(keyId);

                return new DeferredKey(
                    keyId: keyId,
                    creationDate: creationDate,
                    activationDate: activationDate,
                    expirationDate: expirationDate,
                    keyManager: _internalKeyManager,
                    keyElement: keyElement);
            }
            catch (Exception ex)
            {
                WriteKeyDeserializationErrorToLog(ex, keyElement);

                // Don't include this key in the key ring
                return null;
            }
        }

        // returns a Guid (for specific keys) or a DateTimeOffset (for all keys created on or before a specific date)
        private object ProcessRevocationElement(XElement revocationElement)
        {
            Debug.Assert(revocationElement.Name == RevocationElementName);

            try
            {
                string keyIdAsString = (string)revocationElement.Element(KeyElementName).Attribute(IdAttributeName);
                if (keyIdAsString == RevokeAllKeysValue)
                {
                    // this is a mass revocation of all keys as of the specified revocation date
                    DateTimeOffset massRevocationDate = (DateTimeOffset)revocationElement.Element(RevocationDateElementName);
                    _logger?.FoundRevocationOfAllKeysCreatedPriorTo(massRevocationDate);
                    return massRevocationDate;
                }
                else
                {
                    // only one key is being revoked
                    Guid keyId = XmlConvert.ToGuid(keyIdAsString);
                    _logger?.FoundRevocationOfKey(keyId);
                    return keyId;
                }
            }
            catch (Exception ex)
            {
                // Any exceptions that occur are fatal - we don't want to continue if we cannot process
                // revocation information.
                _logger?.ExceptionWhileProcessingRevocationElement(revocationElement, ex);
                throw;
            }
        }

        public void RevokeAllKeys(DateTimeOffset revocationDate, string reason = null)
        {
            // <revocation version="1">
            //   <revocationDate>...</revocationDate>
            //   <!-- ... -->
            //   <key id="*" />
            //   <reason>...</reason>
            // </revocation>

            _logger?.RevokingAllKeysAsOfForReason(revocationDate, reason);

            var revocationElement = new XElement(RevocationElementName,
                new XAttribute(VersionAttributeName, 1),
                new XElement(RevocationDateElementName, revocationDate),
                new XComment(" All keys created before the revocation date are revoked. "),
                new XElement(KeyElementName,
                    new XAttribute(IdAttributeName, RevokeAllKeysValue)),
                new XElement(ReasonElementName, reason));

            // Persist it to the underlying repository and trigger the cancellation token
            string friendlyName = "revocation-" + DateTimeOffsetToFilenameSafeString(revocationDate);
            KeyRepository.StoreElement(revocationElement, friendlyName);
            TriggerAndResetCacheExpirationToken();
        }

        public void RevokeKey(Guid keyId, string reason = null)
        {
            _internalKeyManager.RevokeSingleKey(
                keyId: keyId,
                revocationDate: DateTimeOffset.UtcNow,
                reason: reason);
        }

        private void TriggerAndResetCacheExpirationToken([CallerMemberName] string opName = null, bool suppressLogging = false)
        {
            if (!suppressLogging)
            {
                _logger?.KeyCacheExpirationTokenTriggeredByOperation(opName);
            }

            Interlocked.Exchange(ref _cacheExpirationTokenSource, new CancellationTokenSource())?.Cancel();
        }

        private void WriteKeyDeserializationErrorToLog(Exception error, XElement keyElement)
        {
            // Ideally we'd suppress the error since it might contain sensitive information, but it would be too difficult for
            // an administrator to diagnose the issue if we hide this information. Instead we'll log the error to the error
            // log and the raw <key> element to the debug log. This works for our out-of-box XML decryptors since they don't
            // include sensitive information in the exception message.

            // write sanitized <key> element
            _logger?.ExceptionWhileProcessingKeyElement(keyElement.WithoutChildNodes(), error);

            // write full <key> element
            _logger?.AnExceptionOccurredWhileProcessingElementDebug(keyElement, error);

        }

        IKey IInternalXmlKeyManager.CreateNewKey(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate)
        {
            // <key id="{guid}" version="1">
            //   <creationDate>...</creationDate>
            //   <activationDate>...</activationDate>
            //   <expirationDate>...</expirationDate>
            //   <descriptor deserializerType="{typeName}">
            //     ...
            //   </descriptor>
            // </key>

            _logger?.CreatingKey(keyId, creationDate, activationDate, expirationDate);

            var newDescriptor = _authenticatedEncryptorConfiguration.CreateNewDescriptor()
                ?? CryptoUtil.Fail<IAuthenticatedEncryptorDescriptor>("CreateNewDescriptor returned null.");
            var descriptorXmlInfo = newDescriptor.ExportToXml();

            _logger?.DescriptorDeserializerTypeForKeyIs(keyId, descriptorXmlInfo.DeserializerType.AssemblyQualifiedName);

            // build the <key> element
            var keyElement = new XElement(KeyElementName,
                new XAttribute(IdAttributeName, keyId),
                new XAttribute(VersionAttributeName, 1),
                new XElement(CreationDateElementName, creationDate),
                new XElement(ActivationDateElementName, activationDate),
                new XElement(ExpirationDateElementName, expirationDate),
                new XElement(DescriptorElementName,
                    new XAttribute(DeserializerTypeAttributeName, descriptorXmlInfo.DeserializerType.AssemblyQualifiedName),
                    descriptorXmlInfo.SerializedDescriptorElement));

            // If key escrow policy is in effect, write the *unencrypted* key now.
            if (_keyEscrowSink != null)
            {
                _logger?.KeyEscrowSinkFoundWritingKeyToEscrow(keyId);
            }
            else
            {
                _logger?.NoKeyEscrowSinkFoundNotWritingKeyToEscrow(keyId);
            }
            _keyEscrowSink?.Store(keyId, keyElement);

            // If an XML encryptor has been configured, protect secret key material now.
            if (KeyEncryptor == null)
            {
                _logger?.NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm(keyId);
            }
            var possiblyEncryptedKeyElement = KeyEncryptor?.EncryptIfNecessary(keyElement) ?? keyElement;

            // Persist it to the underlying repository and trigger the cancellation token.
            string friendlyName = Invariant($"key-{keyId:D}");
            KeyRepository.StoreElement(possiblyEncryptedKeyElement, friendlyName);
            TriggerAndResetCacheExpirationToken();

            // And we're done!
            return new Key(
                keyId: keyId,
                creationDate: creationDate,
                activationDate: activationDate,
                expirationDate: expirationDate,
                descriptor: newDescriptor);
        }

        IAuthenticatedEncryptorDescriptor IInternalXmlKeyManager.DeserializeDescriptorFromKeyElement(XElement keyElement)
        {
            try
            {
                // Figure out who will be deserializing this
                XElement descriptorElement = keyElement.Element(DescriptorElementName);
                string descriptorDeserializerTypeName = (string)descriptorElement.Attribute(DeserializerTypeAttributeName);

                // Decrypt the descriptor element and pass it to the descriptor for consumption
                XElement unencryptedInputToDeserializer = descriptorElement.Elements().Single().DecryptElement(_activator);
                var deserializerInstance = _activator.CreateInstance<IAuthenticatedEncryptorDescriptorDeserializer>(descriptorDeserializerTypeName);
                var descriptorInstance = deserializerInstance.ImportFromXml(unencryptedInputToDeserializer);

                return descriptorInstance ?? CryptoUtil.Fail<IAuthenticatedEncryptorDescriptor>("ImportFromXml returned null.");
            }
            catch (Exception ex)
            {
                WriteKeyDeserializationErrorToLog(ex, keyElement);
                throw;
            }
        }

        void IInternalXmlKeyManager.RevokeSingleKey(Guid keyId, DateTimeOffset revocationDate, string reason)
        {
            // <revocation version="1">
            //   <revocationDate>...</revocationDate>
            //   <key id="{guid}" />
            //   <reason>...</reason>
            // </revocation>

            _logger?.RevokingKeyForReason(keyId, revocationDate, reason);

            var revocationElement = new XElement(RevocationElementName,
                new XAttribute(VersionAttributeName, 1),
                new XElement(RevocationDateElementName, revocationDate),
                new XElement(KeyElementName,
                    new XAttribute(IdAttributeName, keyId)),
                new XElement(ReasonElementName, reason));

            // Persist it to the underlying repository and trigger the cancellation token
            string friendlyName = Invariant($"revocation-{keyId:D}");
            KeyRepository.StoreElement(revocationElement, friendlyName);
            TriggerAndResetCacheExpirationToken();
        }
    }
}
