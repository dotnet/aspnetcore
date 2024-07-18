// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

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
    private readonly ITypeNameResolver _typeNameResolver;
    private readonly AlgorithmConfiguration _authenticatedEncryptorConfiguration;
    private readonly IKeyEscrowSink? _keyEscrowSink;
    private readonly IInternalXmlKeyManager _internalKeyManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IEnumerable<IAuthenticatedEncryptorFactory> _encryptorFactories;
    private readonly IDefaultKeyStorageDirectories _keyStorageDirectories;
    private readonly ConcurrentDictionary<Guid, Key> _knownKeyMap = new(); // Grows unboundedly, like the key ring

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
            var keyRepositoryEncryptorPair = GetFallbackKeyRepositoryEncryptorPair();
            keyRepository = keyRepositoryEncryptorPair.Key;
            if (keyEncryptor == null)
            {
                keyEncryptor = keyRepositoryEncryptorPair.Value;
            }
        }

        KeyRepository = keyRepository;
        KeyEncryptor = keyEncryptor;

        _authenticatedEncryptorConfiguration = keyManagementOptions.Value.AuthenticatedEncryptorConfiguration!;

        var escrowSinks = keyManagementOptions.Value.KeyEscrowSinks;
        _keyEscrowSink = escrowSinks.Count > 0 ? new AggregateKeyEscrowSink(escrowSinks) : null;
        _activator = activator;
        // Note: ITypeNameResolver is only implemented on the activator in tests. In production, it's always DefaultTypeNameResolver.
        _typeNameResolver = activator as ITypeNameResolver ?? DefaultTypeNameResolver.Instance;
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

    // Internal for testing
    // Can't use TimeProvider since it's not available in framework
    internal Func<DateTimeOffset> GetUtcNow { get; set; } = () => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public IKey CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate)
    {
        // For an immediately-activated key, the caller's Now may be slightly before ours,
        // so we'll compensate to ensure that activation is never before creation.
        var now = GetUtcNow();
        return _internalKeyManager.CreateNewKey(
            keyId: Guid.NewGuid(),
            creationDate: activationDate < now ? activationDate : now,
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
        var processed = ProcessAllElements(allElements, out _);
        return processed.OfType<IKey>().ToList().AsReadOnly();
    }

    /// <summary>
    /// Returns an array paralleling <paramref name="allElements"/> but:
    ///  1. Key elements become IKeys (with revocation data)
    ///  2. KeyId-based revocations become Guids
    ///  3. Date-based revocations become DateTimeOffsets
    ///  4. Unknown elements become null
    /// </summary>
    private object?[] ProcessAllElements(IReadOnlyCollection<XElement> allElements, out DateTimeOffset? mostRecentMassRevocationDate)
    {
        var elementCount = allElements.Count;

        var results = new object?[elementCount];

        Dictionary<Guid, Key> keyIdToKeyMap = [];
        HashSet<Guid>? revokedKeyIds = null;

        mostRecentMassRevocationDate = null;

        var pos = 0;
        foreach (var element in allElements)
        {
            object? result;
            if (element.Name == KeyElementName)
            {
                // ProcessKeyElement can return null in the case of failure, and if this happens we'll move on.
                // Still need to throw if we see duplicate keys with the same id.
                var key = ProcessKeyElement(element);
                result = key;
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
                result = revocationInfo;
                if (revocationInfo is Guid revocationGuid)
                {
                    // a single key was revoked
                    revokedKeyIds ??= [];
                    if (!revokedKeyIds.Add(revocationGuid))
                    {
                        _logger.KeyRevokedMultipleTimes(revocationGuid);
                    }
                }
                else
                {
                    // all keys as of a certain date were revoked
                    var thisMassRevocationDate = (DateTimeOffset)revocationInfo;
                    if (!mostRecentMassRevocationDate.HasValue || mostRecentMassRevocationDate < thisMassRevocationDate)
                    {
                        mostRecentMassRevocationDate = thisMassRevocationDate;
                    }
                }
            }
            else
            {
                // Skip unknown elements.
                _logger.UnknownElementWithNameFoundInKeyringSkipping(element.Name);
                result = null;
            }

            results[pos++] = result;
        }

        // Apply individual revocations
        if (revokedKeyIds is not null)
        {
            foreach (var revokedKeyId in revokedKeyIds)
            {
                if (keyIdToKeyMap.TryGetValue(revokedKeyId, out var key))
                {
                    key.SetRevoked();
                    _logger.MarkedKeyAsRevokedInTheKeyring(revokedKeyId);
                }
                else
                {
                    _logger.TriedToProcessRevocationOfKeyButNoSuchKeyWasFound(revokedKeyId);
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
                    _logger.MarkedKeyAsRevokedInTheKeyring(key.KeyId);
                }
            }
        }

        // And we're finished!
        return results;
    }

    /// <inheritdoc/>
    public CancellationToken GetCacheExpirationToken()
    {
        Debug.Assert(_cacheExpirationTokenSource != null, $"{nameof(TriggerAndResetCacheExpirationToken)} must have been called first.");

        return Interlocked.CompareExchange<CancellationTokenSource?>(ref _cacheExpirationTokenSource, null, null).Token;
    }

    private Key? ProcessKeyElement(XElement keyElement)
    {
        Debug.Assert(keyElement.Name == KeyElementName);

        try
        {
            // Read metadata and prepare the key for deferred instantiation
            Guid keyId = (Guid)keyElement.Attribute(IdAttributeName)!;

            _logger.FoundKey(keyId);

            if (_knownKeyMap.TryGetValue(keyId, out var oldKey))
            {
                // Keys are immutable (other than revocation), so there's no need to read it again
                return oldKey.Clone();
            }

            DateTimeOffset creationDate = (DateTimeOffset)keyElement.Element(CreationDateElementName)!;
            DateTimeOffset activationDate = (DateTimeOffset)keyElement.Element(ActivationDateElementName)!;
            DateTimeOffset expirationDate = (DateTimeOffset)keyElement.Element(ExpirationDateElementName)!;

            var key = new Key(
                keyId: keyId,
                creationDate: creationDate,
                activationDate: activationDate,
                expirationDate: expirationDate,
                keyManager: this,
                keyElement: keyElement,
                encryptorFactories: _encryptorFactories);

            RecordKey(key);

            return key;
        }
        catch (Exception ex)
        {
            WriteKeyDeserializationErrorToLog(ex, keyElement);

            // Don't include this key in the key ring
            return null;
        }
    }

    private void RecordKey(Key key)
    {
        if (!_knownKeyMap.TryAdd(key.KeyId, key))
        {
            // If we lost the race, the winner inserted an equivalent key
            Debug.Assert(_knownKeyMap.TryGetValue(key.KeyId, out var existingKey));
            Debug.Assert(existingKey.CreationDate == key.CreationDate);
            Debug.Assert(existingKey.ActivationDate == key.ActivationDate);
            Debug.Assert(existingKey.ExpirationDate == key.ExpirationDate);
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
                _logger.FoundRevocationOfAllKeysCreatedPriorTo(massRevocationDate);
                return massRevocationDate;
            }
            else
            {
                // only one key is being revoked
                var keyId = XmlConvert.ToGuid(keyIdAsString);
                _logger.FoundRevocationOfKey(keyId);
                return keyId;
            }
        }
        catch (Exception ex)
        {
            // Any exceptions that occur are fatal - we don't want to continue if we cannot process
            // revocation information.
            _logger.ExceptionWhileProcessingRevocationElement(revocationElement, ex);
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

        _logger.RevokingAllKeysAsOfForReason(revocationDate, reason);

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
            revocationDate: GetUtcNow(),
            reason: reason);
    }

    /// <inheritdoc/>
    public bool CanDeleteKeys => KeyRepository is IDeletableXmlRepository;

    /// <inheritdoc/>
    public bool DeleteKeys(Func<IKey, bool> shouldDelete)
    {
        if (KeyRepository is not IDeletableXmlRepository xmlRepositoryWithDeletion)
        {
            throw Error.XmlKeyManager_DoesNotSupportKeyDeletion();
        }

        return xmlRepositoryWithDeletion.DeleteElements((deletableElements) =>
        {
            // It is important to delete key elements before the corresponding revocation elements,
            // in case the deletion fails part way - we don't want to accidentally unrevoke a key
            // and then not delete it.
            // Start at a non-zero value just to make it a little clearer in the debugger that it
            // was set explicitly.
            const int deletionOrderKey = 1;
            const int deletionOrderRevocation = 2;
            const int deletionOrderMassRevocation = 3;

            var deletableElementsArray = deletableElements.ToArray();

            var allElements = deletableElements.Select(d => d.Element).ToArray();
            var processed = ProcessAllElements(allElements, out var mostRecentMassRevocationDate);

            var allKeyIds = new HashSet<Guid>();
            var deletedKeyIds = new HashSet<Guid>();

            for (var i = 0; i < deletableElementsArray.Length; i++)
            {
                var obj = processed[i];
                if (obj is IKey key)
                {
                    var keyId = key.KeyId;
                    allKeyIds.Add(keyId);

                    if (shouldDelete(key))
                    {
                        _logger.DeletingKey(keyId);
                        deletedKeyIds.Add(keyId);
                        deletableElementsArray[i].DeletionOrder = deletionOrderKey;
                    }
                }
                else if (obj is DateTimeOffset massRevocationDate)
                {
                    if (massRevocationDate < mostRecentMassRevocationDate)
                    {
                        // Delete superceded mass revocation elements
                        deletableElementsArray[i].DeletionOrder = deletionOrderMassRevocation;
                    }
                }
            }

            // Separate loop since deletedKeyIds and allKeyIds need to have been populated.
            for (var i = 0; i < deletableElementsArray.Length; i++)
            {
                if (processed[i] is Guid revocationId)
                {
                    // Delete individual revocations of keys that don't (still) exist
                    if (deletedKeyIds.Contains(revocationId) || !allKeyIds.Contains(revocationId))
                    {
                        deletableElementsArray[i].DeletionOrder = deletionOrderRevocation;
                    }
                }
            }
        });
    }

    private void TriggerAndResetCacheExpirationToken([CallerMemberName] string? opName = null, bool suppressLogging = false)
    {
        if (!suppressLogging)
        {
            _logger.KeyCacheExpirationTokenTriggeredByOperation(opName!);
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
        _logger.ExceptionWhileProcessingKeyElement(keyElement.WithoutChildNodes(), error);

        // write full <key> element
        _logger.AnExceptionOccurredWhileProcessingElementDebug(keyElement, error);
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

        _logger.CreatingKey(keyId, creationDate, activationDate, expirationDate);

        var newDescriptor = _authenticatedEncryptorConfiguration.CreateNewDescriptor()
            ?? CryptoUtil.Fail<IAuthenticatedEncryptorDescriptor>("CreateNewDescriptor returned null.");
        var descriptorXmlInfo = newDescriptor.ExportToXml();

        _logger.DescriptorDeserializerTypeForKeyIs(keyId, descriptorXmlInfo.DeserializerType.AssemblyQualifiedName!);

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
            _logger.KeyEscrowSinkFoundWritingKeyToEscrow(keyId);
        }
        else
        {
            _logger.NoKeyEscrowSinkFoundNotWritingKeyToEscrow(keyId);
        }
        _keyEscrowSink?.Store(keyId, keyElement);

        // If an XML encryptor has been configured, protect secret key material now.
        if (KeyEncryptor == null)
        {
            _logger.NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm(keyId);
        }
        var possiblyEncryptedKeyElement = KeyEncryptor?.EncryptIfNecessary(keyElement) ?? keyElement;

        // Persist it to the underlying repository and trigger the cancellation token.
        var friendlyName = string.Format(CultureInfo.InvariantCulture, "key-{0:D}", keyId);
        KeyRepository.StoreElement(possiblyEncryptedKeyElement, friendlyName);
        TriggerAndResetCacheExpirationToken();

        // And we're done!
        var key = new Key(
            keyId: keyId,
            creationDate: creationDate,
            activationDate: activationDate,
            expirationDate: expirationDate,
            descriptor: newDescriptor,
            encryptorFactories: _encryptorFactories);

        RecordKey(key);

        return key;
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

            var deserializerInstance = CreateDeserializer(descriptorDeserializerTypeName);
            var descriptorInstance = deserializerInstance.ImportFromXml(unencryptedInputToDeserializer);

            return descriptorInstance ?? CryptoUtil.Fail<IAuthenticatedEncryptorDescriptor>("ImportFromXml returned null.");
        }
        catch (Exception ex)
        {
            WriteKeyDeserializationErrorToLog(ex, keyElement);
            throw;
        }
    }

    private IAuthenticatedEncryptorDescriptorDeserializer CreateDeserializer(string descriptorDeserializerTypeName)
    {
        // typeNameToMatch will be used for matching against known types but not passed to the activator.
        // The activator will do its own forwarding.
        var typeNameToMatch = TypeForwardingActivator.TryForwardTypeName(descriptorDeserializerTypeName, out var forwardedTypeName)
            ? forwardedTypeName
            : descriptorDeserializerTypeName;

        if (typeof(AuthenticatedEncryptorDescriptorDeserializer).MatchName(typeNameToMatch, _typeNameResolver))
        {
            return _activator.CreateInstance<AuthenticatedEncryptorDescriptorDeserializer>(descriptorDeserializerTypeName);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && typeof(CngCbcAuthenticatedEncryptorDescriptorDeserializer).MatchName(typeNameToMatch, _typeNameResolver))
        {
            return _activator.CreateInstance<CngCbcAuthenticatedEncryptorDescriptorDeserializer>(descriptorDeserializerTypeName);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && typeof(CngGcmAuthenticatedEncryptorDescriptorDeserializer).MatchName(typeNameToMatch, _typeNameResolver))
        {
            return _activator.CreateInstance<CngGcmAuthenticatedEncryptorDescriptorDeserializer>(descriptorDeserializerTypeName);
        }
        else if (typeof(ManagedAuthenticatedEncryptorDescriptorDeserializer).MatchName(typeNameToMatch, _typeNameResolver))
        {
            return _activator.CreateInstance<ManagedAuthenticatedEncryptorDescriptorDeserializer>(descriptorDeserializerTypeName);
        }

        return _activator.CreateInstance<IAuthenticatedEncryptorDescriptorDeserializer>(descriptorDeserializerTypeName);
    }

    void IInternalXmlKeyManager.RevokeSingleKey(Guid keyId, DateTimeOffset revocationDate, string? reason)
    {
        // <revocation version="1">
        //   <revocationDate>...</revocationDate>
        //   <key id="{guid}" />
        //   <reason>...</reason>
        // </revocation>

        _logger.RevokingKeyForReason(keyId, revocationDate, reason);

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
        IXmlRepository? repository;
        IXmlEncryptor? encryptor = null;

        // If we're running in Azure Web Sites, the key repository goes in the %HOME% directory.
        var azureWebSitesKeysFolder = _keyStorageDirectories.GetKeyStorageDirectoryForAzureWebSites();
        if (azureWebSitesKeysFolder != null)
        {
            _logger.UsingAzureAsKeyRepository(azureWebSitesKeysFolder.FullName);

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
                    _logger.UsingProfileAsKeyRepositoryWithDPAPI(localAppDataKeysFolder.FullName);
                }
                else
                {
                    _logger.UsingProfileAsKeyRepository(localAppDataKeysFolder.FullName);
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

                    _logger.UsingRegistryAsKeyRepositoryWithDPAPI(regKeyStorageKey!.Name);
                }
                else
                {
                    // Final fallback - use an ephemeral repository since we don't know where else to go.
                    // This can only be used for development scenarios.
                    repository = new EphemeralXmlRepository(_loggerFactory);

                    _logger.UsingEphemeralKeyRepository();
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
}
