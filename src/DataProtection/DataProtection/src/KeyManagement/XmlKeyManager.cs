// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    /// <summary>
    /// A key manager backed by an <see cref="IXmlRepository"/>.
    /// </summary>
    public sealed partial class XmlKeyManager : IKeyManager, IInternalXmlKeyManager
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
        private readonly AlgorithmConfiguration _authenticatedEncryptorConfiguration;
        private readonly IKeyEscrowSink? _keyEscrowSink;
        private readonly IInternalXmlKeyManager _internalKeyManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IEnumerable<IAuthenticatedEncryptorFactory> _encryptorFactories;
        private readonly IDefaultKeyStorageDirectories _keyStorageDirectories;

        private CancellationTokenSource? _cacheExpirationTokenSource;

        /// <summary>
        /// Creates an <see cref="XmlKeyManager"/>.
        /// </summary>
        /// <param name="keyManagementOptions">The <see cref="IOptions{KeyManagementOptions}"/> instance that provides the configuration.</param>
        /// <param name="activator">The <see cref="IActivator"/>.</param>
#pragma warning disable PUB0001 // Pubternal type IActivator in public API
        public XmlKeyManager(IOptions<KeyManagementOptions> keyManagementOptions, IActivator activator)
#pragma warning restore PUB0001 // Pubternal type IActivator in public API
            : this(keyManagementOptions, activator, NullLoggerFactory.Instance)
        { }

        /// <summary>
        /// Creates an <see cref="XmlKeyManager"/>.
        /// </summary>
        /// <param name="keyManagementOptions">The <see cref="IOptions{KeyManagementOptions}"/> instance that provides the configuration.</param>
        /// <param name="activator">The <see cref="IActivator"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
#pragma warning disable PUB0001 // Pubternal type IActivator in public API
        public XmlKeyManager(IOptions<KeyManagementOptions> keyManagementOptions, IActivator activator, ILoggerFactory loggerFactory)
#pragma warning restore PUB0001 // Pubternal type IActivator in public API
            : this(keyManagementOptions, activator, loggerFactory, DefaultKeyStorageDirectories.Instance)
        { }

        internal XmlKeyManager(
            IOptions<KeyManagementOptions> keyManagementOptions,
            IActivator activator,
            ILoggerFactory loggerFactory,
            IDefaultKeyStorageDirectories keyStorageDirectories)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<XmlKeyManager>();
            _keyStorageDirectories = keyStorageDirectories ?? throw new ArgumentNullException(nameof(keyStorageDirectories));

            var keyRepository = keyManagementOptions.Value.XmlRepository;
            var keyEncryptor = keyManagementOptions.Value.XmlEncryptor;
            if (keyRepository == null)
            {
                if (keyEncryptor != null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatXmlKeyManager_IXmlRepositoryNotFound(nameof(IXmlRepository), nameof(IXmlEncryptor)));
                }
                else
                {
                    var keyRepositoryEncryptorPair = GetFallbackKeyRepositoryEncryptorPair();
                    keyRepository = keyRepositoryEncryptorPair.Key;
                    keyEncryptor = keyRepositoryEncryptorPair.Value;
                }
            }

            KeyRepository = keyRepository;
            KeyEncryptor = keyEncryptor;

            _authenticatedEncryptorConfiguration = keyManagementOptions.Value.AuthenticatedEncryptorConfiguration!;

            var escrowSinks = keyManagementOptions.Value.KeyEscrowSinks;
            _keyEscrowSink = escrowSinks.Count > 0 ? new AggregateKeyEscrowSink(escrowSinks) : null;
            _activator = activator;
            TriggerAndResetCacheExpirationToken(suppressLogging: true);
            _internalKeyManager = _internalKeyManager ?? this;
            _encryptorFactories = keyManagementOptions.Value.AuthenticatedEncryptorFactories;
        }

        // Internal for testing.
        internal XmlKeyManager(
            IOptions<KeyManagementOptions> keyManagementOptions,
            IActivator activator,
            ILoggerFactory loggerFactory,
            IInternalXmlKeyManager internalXmlKeyManager)
            : this(keyManagementOptions, activator, loggerFactory)
        {
            _internalKeyManager = internalXmlKeyManager;
        }

        internal IXmlEncryptor? KeyEncryptor { get; }

        internal IXmlRepository KeyRepository { get; }

        /// <inheritdoc />
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
            return dateTime.UtcDateTime.ToString("yyyyMMddTHHmmssFFFFFFFZ", CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IKey> GetAllKeys()
        {
            var allElements = KeyRepository.GetAllElements();

            // We aggregate all the information we read into three buckets
            Dictionary<Guid, KeyBase> keyIdToKeyMap = new Dictionary<Guid, KeyBase>();
            HashSet<Guid>? revokedKeyIds = null;
            DateTimeOffset? mostRecentMassRevocationDate = null;

            foreach (var element in allElements)
            {
                if (element.Name == KeyElementName)
                {
                    // ProcessKeyElement can return null in the case of failure, and if this happens we'll move on.
                    // Still need to throw if we see duplicate keys with the same id.
                    var key = ProcessKeyElement(element);
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
                    var revocationInfo = ProcessRevocationElement(element);
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
                    Log.UnknownElementWithNameFoundInKeyringSkipping(_logger, element.Name);
                }
            }

            // Apply individual revocations
            if (revokedKeyIds != null)
            {
                foreach (Guid revokedKeyId in revokedKeyIds)
                {
                    keyIdToKeyMap.TryGetValue(revokedKeyId, out var key);
                    if (key != null)
                    {
                        key.SetRevoked();
                        Log.MarkedKeyAsRevokedInTheKeyring(_logger, revokedKeyId);
                    }
                    else
                    {
                        Log.TriedToProcessRevocationOfKeyButNoSuchKeyWasFound(_logger, revokedKeyId);
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
                        Log.MarkedKeyAsRevokedInTheKeyring(_logger, key.KeyId);
                    }
                }
            }

            // And we're finished!
            return keyIdToKeyMap.Values.ToList().AsReadOnly();
        }

        /// <inheritdoc/>
        public CancellationToken GetCacheExpirationToken()
        {
            Debug.Assert(_cacheExpirationTokenSource != null, $"{nameof(TriggerAndResetCacheExpirationToken)} must have been called first.");

            return Interlocked.CompareExchange<CancellationTokenSource?>(ref _cacheExpirationTokenSource, null, null).Token;
        }

        private KeyBase? ProcessKeyElement(XElement keyElement)
        {
            Debug.Assert(keyElement.Name == KeyElementName);

            try
            {
                // Read metadata and prepare the key for deferred instantiation
                Guid keyId = (Guid)keyElement.Attribute(IdAttributeName)!;
                DateTimeOffset creationDate = (DateTimeOffset)keyElement.Element(CreationDateElementName)!;
                DateTimeOffset activationDate = (DateTimeOffset)keyElement.Element(ActivationDateElementName)!;
                DateTimeOffset expirationDate = (DateTimeOffset)keyElement.Element(ExpirationDateElementName)!;
                Log.FoundKey(_logger, keyId);

                return new DeferredKey(
                    keyId: keyId,
                    creationDate: creationDate,
                    activationDate: activationDate,
                    expirationDate: expirationDate,
                    keyManager: this,
                    keyElement: keyElement,
                    encryptorFactories: _encryptorFactories);
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
                string keyIdAsString = (string)revocationElement.Element(KeyElementName)!.Attribute(IdAttributeName)!;
                if (keyIdAsString == RevokeAllKeysValue)
                {
                    // this is a mass revocation of all keys as of the specified revocation date
                    DateTimeOffset massRevocationDate = (DateTimeOffset)revocationElement.Element(RevocationDateElementName)!;
                    Log.FoundRevocationOfAllKeysCreatedPriorTo(_logger, massRevocationDate);
                    return massRevocationDate;
                }
                else
                {
                    // only one key is being revoked
                    var keyId = XmlConvert.ToGuid(keyIdAsString);
                    Log.FoundRevocationOfKey(_logger, keyId);
                    return keyId;
                }
            }
            catch (Exception ex)
            {
                // Any exceptions that occur are fatal - we don't want to continue if we cannot process
                // revocation information.
                Log.ExceptionWhileProcessingRevocationElement(_logger, revocationElement, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public void RevokeAllKeys(DateTimeOffset revocationDate, string? reason = null)
        {
            // <revocation version="1">
            //   <revocationDate>...</revocationDate>
            //   <!-- ... -->
            //   <key id="*" />
            //   <reason>...</reason>
            // </revocation>
            Log.RevokingAllKeysAsOfForReason(_logger, revocationDate, reason);

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

        /// <inheritdoc/>
        public void RevokeKey(Guid keyId, string? reason = null)
        {
            _internalKeyManager.RevokeSingleKey(
                keyId: keyId,
                revocationDate: DateTimeOffset.UtcNow,
                reason: reason);
        }

        private void TriggerAndResetCacheExpirationToken([CallerMemberName] string? opName = null, bool suppressLogging = false)
        {
            if (!suppressLogging)
            {
                Log.KeyCacheExpirationTokenTriggeredByOperation(_logger, opName!);
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
            Log.ExceptionWhileProcessingKeyElement(_logger, keyElement.WithoutChildNodes(), error);
            // write full <key> element
            Log.AnExceptionOccurredWhileProcessingElementDebug(_logger, keyElement, error);

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

            Log.CreatingKey(_logger, keyId, creationDate, activationDate, expirationDate);

            var newDescriptor = _authenticatedEncryptorConfiguration.CreateNewDescriptor()
                ?? CryptoUtil.Fail<IAuthenticatedEncryptorDescriptor>("CreateNewDescriptor returned null.");
            var descriptorXmlInfo = newDescriptor.ExportToXml();
            Log.DescriptorDeserializerTypeForKeyIs(_logger, keyId, descriptorXmlInfo.DeserializerType.AssemblyQualifiedName!);

            // build the <key> element
            var keyElement = new XElement(KeyElementName,
                new XAttribute(IdAttributeName, keyId),
                new XAttribute(VersionAttributeName, 1),
                new XElement(CreationDateElementName, creationDate),
                new XElement(ActivationDateElementName, activationDate),
                new XElement(ExpirationDateElementName, expirationDate),
                new XElement(DescriptorElementName,
                    new XAttribute(DeserializerTypeAttributeName, descriptorXmlInfo.DeserializerType.AssemblyQualifiedName!),
                    descriptorXmlInfo.SerializedDescriptorElement));

            // If key escrow policy is in effect, write the *unencrypted* key now.
            if (_keyEscrowSink != null)
            {
                Log.KeyEscrowSinkFoundWritingKeyToEscrow(_logger, keyId);
            }
            else
            {
                Log.NoKeyEscrowSinkFoundNotWritingKeyToEscrow(_logger, keyId);
            }
            _keyEscrowSink?.Store(keyId, keyElement);

            // If an XML encryptor has been configured, protect secret key material now.
            if (KeyEncryptor == null)
            {
                Log.NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm(_logger, keyId);
            }
            var possiblyEncryptedKeyElement = KeyEncryptor?.EncryptIfNecessary(keyElement) ?? keyElement;

            // Persist it to the underlying repository and trigger the cancellation token.
            var friendlyName = string.Format(CultureInfo.InvariantCulture, "key-{0:D}", keyId);
            KeyRepository.StoreElement(possiblyEncryptedKeyElement, friendlyName);
            TriggerAndResetCacheExpirationToken();

            // And we're done!
            return new Key(
                keyId: keyId,
                creationDate: creationDate,
                activationDate: activationDate,
                expirationDate: expirationDate,
                descriptor: newDescriptor,
                encryptorFactories: _encryptorFactories);
        }

        IAuthenticatedEncryptorDescriptor IInternalXmlKeyManager.DeserializeDescriptorFromKeyElement(XElement keyElement)
        {
            try
            {
                // Figure out who will be deserializing this
                var descriptorElement = keyElement.Element(DescriptorElementName);
                string descriptorDeserializerTypeName = (string)descriptorElement!.Attribute(DeserializerTypeAttributeName)!;

                // Decrypt the descriptor element and pass it to the descriptor for consumption
                var unencryptedInputToDeserializer = descriptorElement.Elements().Single().DecryptElement(_activator);
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

        void IInternalXmlKeyManager.RevokeSingleKey(Guid keyId, DateTimeOffset revocationDate, string? reason)
        {
            // <revocation version="1">
            //   <revocationDate>...</revocationDate>
            //   <key id="{guid}" />
            //   <reason>...</reason>
            // </revocation>
            Log.RevokingKeyForReason(_logger, keyId, revocationDate, reason);

            var revocationElement = new XElement(RevocationElementName,
                new XAttribute(VersionAttributeName, 1),
                new XElement(RevocationDateElementName, revocationDate),
                new XElement(KeyElementName,
                    new XAttribute(IdAttributeName, keyId)),
                new XElement(ReasonElementName, reason));

            // Persist it to the underlying repository and trigger the cancellation token
            var friendlyName = string.Format(CultureInfo.InvariantCulture, "revocation-{0:D}", keyId);
            KeyRepository.StoreElement(revocationElement, friendlyName);
            TriggerAndResetCacheExpirationToken();
        }

        internal KeyValuePair<IXmlRepository, IXmlEncryptor?> GetFallbackKeyRepositoryEncryptorPair()
        {
            IXmlRepository? repository = null;
            IXmlEncryptor? encryptor = null;

            // If we're running in Azure Web Sites, the key repository goes in the %HOME% directory.
            var azureWebSitesKeysFolder = _keyStorageDirectories.GetKeyStorageDirectoryForAzureWebSites();
            if (azureWebSitesKeysFolder != null)
            {
                Log.UsingAzureAsKeyRepository(_logger, azureWebSitesKeysFolder.FullName);

                // Cloud DPAPI isn't yet available, so we don't encrypt keys at rest.
                // This isn't all that different than what Azure Web Sites does today, and we can always add this later.
                repository = new FileSystemXmlRepository(azureWebSitesKeysFolder, _loggerFactory);
            }
            else
            {
                // If the user profile is available, store keys in the user profile directory.
                var localAppDataKeysFolder = _keyStorageDirectories.GetKeyStorageDirectory();
                if (localAppDataKeysFolder != null)
                {
                    if (OSVersionUtil.IsWindows())
                    {
                        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)); // Hint for the platform compatibility analyzer.

                        // If the user profile is available, we can protect using DPAPI.
                        // Probe to see if protecting to local user is available, and use it as the default if so.
                        encryptor = new DpapiXmlEncryptor(
                            protectToLocalMachine: !DpapiSecretSerializerHelper.CanProtectToCurrentUserAccount(),
                            loggerFactory: _loggerFactory);
                    }
                    repository = new FileSystemXmlRepository(localAppDataKeysFolder, _loggerFactory);

                    if (encryptor != null)
                    {
                        Log.UsingProfileAsKeyRepositoryWithDPAPI(_logger, localAppDataKeysFolder.FullName);
                    }
                    else
                    {
                        Log.UsingProfileAsKeyRepository(_logger, localAppDataKeysFolder.FullName);
                    }
                }
                else
                {
                    // Use profile isn't available - can we use the HKLM registry?
                    RegistryKey? regKeyStorageKey = null;
                    if (OSVersionUtil.IsWindows())
                    {
                        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)); // Hint for the platform compatibility analyzer.
                        regKeyStorageKey = RegistryXmlRepository.DefaultRegistryKey;
                    }
                    if (regKeyStorageKey != null)
                    {
                        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)); // Hint for the platform compatibility analyzer.
                        regKeyStorageKey = RegistryXmlRepository.DefaultRegistryKey;

                        // If the user profile isn't available, we can protect using DPAPI (to machine).
                        encryptor = new DpapiXmlEncryptor(protectToLocalMachine: true, loggerFactory: _loggerFactory);
                        repository = new RegistryXmlRepository(regKeyStorageKey!, _loggerFactory);
                        Log.UsingRegistryAsKeyRepositoryWithDPAPI(_logger, regKeyStorageKey!.Name);
                    }
                    else
                    {
                        // Final fallback - use an ephemeral repository since we don't know where else to go.
                        // This can only be used for development scenarios.
                        repository = new EphemeralXmlRepository(_loggerFactory);
                        Log.UsingEphemeralKeyRepository(_logger);
                    }
                }
            }

            return new KeyValuePair<IXmlRepository, IXmlEncryptor?>(repository, encryptor);
        }

        private sealed class AggregateKeyEscrowSink : IKeyEscrowSink
        {
            private readonly IList<IKeyEscrowSink> _sinks;

            public AggregateKeyEscrowSink(IList<IKeyEscrowSink> sinks)
            {
                _sinks = sinks;
            }

            public void Store(Guid keyId, XElement element)
            {
                foreach (var sink in _sinks)
                {
                    sink.Store(keyId, element);
                }
            }
        }

        private partial class Log
        {
            [LoggerMessage(15, LogLevel.Warning, "Unknown element with name '{Name}' found in keyring, skipping.", EventName = "UnknownElementWithNameFoundInKeyringSkipping")]
            public static partial void UnknownElementWithNameFoundInKeyringSkipping(ILogger logger, XName name);

            [LoggerMessage(16, LogLevel.Debug, "Marked key {KeyId:B} as revoked in the keyring.", EventName = "MarkedKeyAsRevokedInTheKeyring")]
            public static partial void MarkedKeyAsRevokedInTheKeyring(ILogger logger, Guid keyId);

            [LoggerMessage(17, LogLevel.Warning, "Tried to process revocation of key {KeyId:B}, but no such key was found in keyring. Skipping.", EventName = "TriedToProcessRevocationOfKeyButNoSuchKeyWasFound")]
            public static partial void TriedToProcessRevocationOfKeyButNoSuchKeyWasFound(ILogger logger, Guid keyId);

            [LoggerMessage(18, LogLevel.Debug, "Found key {KeyId:B}.", EventName = "FoundKey")]
            public static partial void FoundKey(ILogger logger, Guid keyId);

            [LoggerMessage(19, LogLevel.Debug, "Found revocation of all keys created prior to {RevocationDate:u}.", EventName = "FoundRevocationOfAllKeysCreatedPriorTo")]
            public static partial void FoundRevocationOfAllKeysCreatedPriorTo(ILogger logger, DateTimeOffset revocationDate);

            [LoggerMessage(20, LogLevel.Debug, "Found revocation of key {KeyId:B}.", EventName = "FoundRevocationOfKey")]
            public static partial void FoundRevocationOfKey(ILogger logger, Guid keyId);

            [LoggerMessage(21, LogLevel.Error, "An exception occurred while processing the revocation element '{RevocationElement}'. Cannot continue keyring processing.", EventName = "ExceptionWhileProcessingRevocationElement")]
            public static partial void ExceptionWhileProcessingRevocationElement(ILogger logger, XElement revocationElement, Exception exception);

            [LoggerMessage(22, LogLevel.Information, "Revoking all keys as of {RevocationDate:u} for reason '{Reason}'.", EventName = "RevokingAllKeysAsOfForReason")]
            public static partial void RevokingAllKeysAsOfForReason(ILogger logger, DateTimeOffset revocationDate, string? reason);

            [LoggerMessage(23, LogLevel.Debug, "Key cache expiration token triggered by '{OperationName}' operation.", EventName = "KeyCacheExpirationTokenTriggeredByOperation")]
            public static partial void KeyCacheExpirationTokenTriggeredByOperation(ILogger logger, string operationName);

            [LoggerMessage(24, LogLevel.Error, "An exception occurred while processing the key element '{Element}'.", EventName = "ExceptionOccurredWhileProcessingTheKeyElement")]
            public static partial void ExceptionWhileProcessingKeyElement(ILogger logger, XElement element, Exception exception);

            [LoggerMessage(25, LogLevel.Trace, "An exception occurred while processing the key element '{Element}'.", EventName = "ExceptionOccurredWhileProcessingTheKeyElementDebug")]
            public static partial void AnExceptionOccurredWhileProcessingElementDebug(ILogger logger, XElement element, Exception exception);

            [LoggerMessage(32, LogLevel.Debug, "Descriptor deserializer type for key {KeyId:B} is '{AssemblyQualifiedName}'.", EventName = "DescriptorDeserializerTypeForKeyIs")]
            public static partial void DescriptorDeserializerTypeForKeyIs(ILogger logger, Guid keyId, string assemblyQualifiedName);

            [LoggerMessage(33, LogLevel.Debug, "Key escrow sink found. Writing key {KeyId:B} to escrow.", EventName = "KeyEscrowSinkFoundWritingKeyToEscrow")]
            public static partial void KeyEscrowSinkFoundWritingKeyToEscrow(ILogger logger, Guid keyId);

            [LoggerMessage(34, LogLevel.Debug, "No key escrow sink found. Not writing key {KeyId:B} to escrow.", EventName = "NoKeyEscrowSinkFoundNotWritingKeyToEscrow")]
            public static partial void NoKeyEscrowSinkFoundNotWritingKeyToEscrow(ILogger logger, Guid keyId);

            [LoggerMessage(35, LogLevel.Warning, "No XML encryptor configured. Key {KeyId:B} may be persisted to storage in unencrypted form.", EventName = "NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm")]
            public static partial void NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm(ILogger logger, Guid keyId);

            [LoggerMessage(36, LogLevel.Information, "Revoking key {KeyId:B} at {RevocationDate:u} for reason '{Reason}'.", EventName = "RevokingKeyForReason")]
            public static partial void RevokingKeyForReason(ILogger logger, Guid keyId, DateTimeOffset revocationDate, string? reason);

            [LoggerMessage(58, LogLevel.Information, "Creating key {KeyId:B} with creation date {CreationDate:u}, activation date {ActivationDate:u}, and expiration date {ExpirationDate:u}.", EventName = "CreatingKey")]
            public static partial void CreatingKey(ILogger logger, Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate);

            [LoggerMessage(59, LogLevel.Warning, "Neither user profile nor HKLM registry available. Using an ephemeral key repository. Protected data will be unavailable when application exits.", EventName = "UsingEphemeralKeyRepository")]
            public static partial void UsingEphemeralKeyRepository(ILogger logger);

            [LoggerMessage(61, LogLevel.Information, "User profile not available. Using '{Name}' as key repository and Windows DPAPI to encrypt keys at rest.", EventName = "UsingRegistryAsKeyRepositoryWithDPAPI")]
            public static partial void UsingRegistryAsKeyRepositoryWithDPAPI(ILogger logger, string name);

            [LoggerMessage(62, LogLevel.Information, "User profile is available. Using '{FullName}' as key repository; keys will not be encrypted at rest.", EventName = "UsingProfileAsKeyRepository")]
            public static partial void UsingProfileAsKeyRepository(ILogger logger, string fullName);

            [LoggerMessage(63, LogLevel.Information, "User profile is available. Using '{FullName}' as key repository and Windows DPAPI to encrypt keys at rest.", EventName = "UsingProfileAsKeyRepositoryWithDPAPI")]
            public static partial void UsingProfileAsKeyRepositoryWithDPAPI(ILogger logger, string fullName);

            [LoggerMessage(64, LogLevel.Information, "Azure Web Sites environment detected. Using '{FullName}' as key repository; keys will not be encrypted at rest.", EventName = "UsingAzureAsKeyRepository")]
            public static partial void UsingAzureAsKeyRepository(ILogger logger, string fullName);
        }
    }
}
