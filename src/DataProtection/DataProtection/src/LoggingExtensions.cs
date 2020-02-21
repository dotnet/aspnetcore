// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Win32;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Helpful extension methods on <see cref="ILogger"/>.
    /// </summary>
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Guid, DateTimeOffset, Exception> _usingFallbackKeyWithExpirationAsDefaultKey;

        private static Action<ILogger, Guid, Exception> _usingKeyAsDefaultKey;

        private static Action<ILogger, string, string, Exception> _openingCNGAlgorithmFromProviderWithHMAC;

        private static Action<ILogger, string, string, Exception> _openingCNGAlgorithmFromProviderWithChainingModeCBC;

        private static Action<ILogger, Guid, string, Exception> _performingUnprotectOperationToKeyWithPurposes;

        private static Action<ILogger, Guid, Exception> _keyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed;

        private static Action<ILogger, Guid, Exception> _keyWasRevokedCallerRequestedUnprotectOperationProceedRegardless;

        private static Action<ILogger, Guid, Exception> _keyWasRevokedUnprotectOperationCannotProceed;

        private static Action<ILogger, string, string, Exception> _openingCNGAlgorithmFromProviderWithChainingModeGCM;

        private static Action<ILogger, string, Exception> _usingManagedKeyedHashAlgorithm;

        private static Action<ILogger, string, Exception> _usingManagedSymmetricAlgorithm;

        private static Action<ILogger, Guid, string, Exception> _keyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed;

        private static Action<ILogger, Guid, DateTimeOffset, Exception> _consideringKeyWithExpirationDateAsDefaultKey;

        private static Action<ILogger, Guid, Exception> _keyIsNoLongerUnderConsiderationAsDefault;

        private static Action<ILogger, XName, Exception> _unknownElementWithNameFoundInKeyringSkipping;

        private static Action<ILogger, Guid, Exception> _markedKeyAsRevokedInTheKeyring;

        private static Action<ILogger, Guid, Exception> _triedToProcessRevocationOfKeyButNoSuchKeyWasFound;

        private static Action<ILogger, Guid, Exception> _foundKey;

        private static Action<ILogger, DateTimeOffset, Exception> _foundRevocationOfAllKeysCreatedPriorTo;

        private static Action<ILogger, Guid, Exception> _foundRevocationOfKey;

        private static Action<ILogger, XElement, Exception> _exceptionWhileProcessingRevocationElement;

        private static Action<ILogger, DateTimeOffset, string, Exception> _revokingAllKeysAsOfForReason;

        private static Action<ILogger, string, Exception> _keyCacheExpirationTokenTriggeredByOperation;

        private static Action<ILogger, XElement, Exception> _anExceptionOccurredWhileProcessingTheKeyElement;

        private static Action<ILogger, XElement, Exception> _anExceptionOccurredWhileProcessingTheKeyElementDebug;

        private static Action<ILogger, string, Exception> _encryptingToWindowsDPAPIForCurrentUserAccount;

        private static Action<ILogger, string, Exception> _encryptingToWindowsDPAPINGUsingProtectionDescriptorRule;

        private static Action<ILogger, string, Exception> _anErrorOccurredWhileEncryptingToX509CertificateWithThumbprint;

        private static Action<ILogger, string, Exception> _encryptingToX509CertificateWithThumbprint;

        private static Action<ILogger, string, Exception> _exceptionOccurredWhileTryingToResolveCertificateWithThumbprint;

        private static Action<ILogger, Guid, string, Exception> _performingProtectOperationToKeyWithPurposes;

        private static Action<ILogger, Guid, DateTimeOffset, DateTimeOffset, DateTimeOffset, Exception> _creatingKey;

        private static Action<ILogger, Guid, string, Exception> _descriptorDeserializerTypeForKeyIs;

        private static Action<ILogger, Guid, Exception> _keyEscrowSinkFoundWritingKeyToEscrow;

        private static Action<ILogger, Guid, Exception> _noKeyEscrowSinkFoundNotWritingKeyToEscrow;

        private static Action<ILogger, Guid, Exception> _noXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm;

        private static Action<ILogger, Guid, DateTimeOffset, string, Exception> _revokingKeyForReason;

        private static Action<ILogger, string, Exception> _readingDataFromFile;

        private static Action<ILogger, string, string, Exception> _nameIsNotSafeFileName;

        private static Action<ILogger, string, Exception> _writingDataToFile;

        private static Action<ILogger, RegistryKey, string, Exception> _readingDataFromRegistryKeyValue;

        private static Action<ILogger, string, string, Exception> _nameIsNotSafeRegistryValueName;

        private static Action<ILogger, string, Exception> _decryptingSecretElementUsingWindowsDPAPING;

        private static Action<ILogger, Exception> _exceptionOccurredTryingToDecryptElement;

        private static Action<ILogger, Exception> _encryptingUsingNullEncryptor;

        private static Action<ILogger, Exception> _usingEphemeralDataProtectionProvider;

        private static Action<ILogger, Exception> _existingCachedKeyRingIsExpiredRefreshing;

        private static Action<ILogger, Exception> _errorOccurredWhileRefreshingKeyRing;

        private static Action<ILogger, Exception> _errorOccurredWhileReadingKeyRing;

        private static Action<ILogger, Exception> _keyRingDoesNotContainValidDefaultKey;

        private static Action<ILogger, Exception> _usingInmemoryRepository;

        private static Action<ILogger, Exception> _decryptingSecretElementUsingWindowsDPAPI;

        private static Action<ILogger, Exception> _defaultKeyExpirationImminentAndRepository;

        private static Action<ILogger, Exception> _repositoryContainsNoViableDefaultKey;

        private static Action<ILogger, Exception> _errorOccurredWhileEncryptingToWindowsDPAPI;

        private static Action<ILogger, Exception> _encryptingToWindowsDPAPIForLocalMachineAccount;

        private static Action<ILogger, Exception> _errorOccurredWhileEncryptingToWindowsDPAPING;

        private static Action<ILogger, Exception> _policyResolutionStatesThatANewKeyShouldBeAddedToTheKeyRing;

        private static Action<ILogger, Guid, Exception> _keyRingWasLoadedOnStartup;

        private static Action<ILogger, Exception> _keyRingFailedToLoadOnStartup;

        private static Action<ILogger, Exception> _usingEphemeralKeyRepository;

        private static Action<ILogger, string, Exception> _usingRegistryAsKeyRepositoryWithDPAPI;

        private static Action<ILogger, string, Exception> _usingProfileAsKeyRepository;

        private static Action<ILogger, string, Exception> _usingProfileAsKeyRepositoryWithDPAPI;

        private static Action<ILogger, string, Exception> _usingAzureAsKeyRepository;

        private static Action<ILogger, string, Exception> _usingEphemeralFileSystemLocationInContainer;

        static LoggingExtensions()
        {
            _usingFallbackKeyWithExpirationAsDefaultKey = LoggerMessage.Define<Guid, DateTimeOffset>(
                eventId: new EventId(1, "UsingFallbackKeyWithExpirationAsDefaultKey"),
                logLevel: LogLevel.Warning,
                formatString: "Policy resolution states that a new key should be added to the key ring, but automatic generation of keys is disabled. Using fallback key {KeyId:B} with expiration {ExpirationDate:u} as default key.");
            _usingKeyAsDefaultKey = LoggerMessage.Define<Guid>(
                eventId: new EventId(2, "UsingKeyAsDefaultKey"),
                logLevel: LogLevel.Debug,
                formatString: "Using key {KeyId:B} as the default key.");
            _openingCNGAlgorithmFromProviderWithHMAC = LoggerMessage.Define<string, string>(
                eventId: new EventId(3, "OpeningCNGAlgorithmFromProviderWithHMAC"),
                logLevel: LogLevel.Debug,
                formatString: "Opening CNG algorithm '{HashAlgorithm}' from provider '{HashAlgorithmProvider}' with HMAC.");
            _openingCNGAlgorithmFromProviderWithChainingModeCBC = LoggerMessage.Define<string, string>(
                eventId: new EventId(4, "OpeningCNGAlgorithmFromProviderWithChainingModeCBC"),
                logLevel: LogLevel.Debug,
                formatString: "Opening CNG algorithm '{EncryptionAlgorithm}' from provider '{EncryptionAlgorithmProvider}' with chaining mode CBC.");
            _performingUnprotectOperationToKeyWithPurposes = LoggerMessage.Define<Guid, string>(
                eventId: new EventId(5, "PerformingUnprotectOperationToKeyWithPurposes"),
                logLevel: LogLevel.Trace,
                formatString: "Performing unprotect operation to key {KeyId:B} with purposes {Purposes}.");
            _keyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed = LoggerMessage.Define<Guid>(
                eventId: new EventId(6, "KeyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed"),
                logLevel: LogLevel.Trace,
                formatString: "Key {KeyId:B} was not found in the key ring. Unprotect operation cannot proceed.");
            _keyWasRevokedCallerRequestedUnprotectOperationProceedRegardless = LoggerMessage.Define<Guid>(
                eventId: new EventId(7, "KeyWasRevokedCallerRequestedUnprotectOperationProceedRegardless"),
                logLevel: LogLevel.Debug,
                formatString: "Key {KeyId:B} was revoked. Caller requested unprotect operation proceed regardless.");
            _keyWasRevokedUnprotectOperationCannotProceed = LoggerMessage.Define<Guid>(
                eventId: new EventId(8, "KeyWasRevokedUnprotectOperationCannotProceed"),
                logLevel: LogLevel.Debug,
                formatString: "Key {KeyId:B} was revoked. Unprotect operation cannot proceed.");
            _openingCNGAlgorithmFromProviderWithChainingModeGCM = LoggerMessage.Define<string, string>(
                eventId: new EventId(9, "OpeningCNGAlgorithmFromProviderWithChainingModeGCM"),
                logLevel: LogLevel.Debug,
                formatString: "Opening CNG algorithm '{EncryptionAlgorithm}' from provider '{EncryptionAlgorithmProvider}' with chaining mode GCM.");
            _usingManagedKeyedHashAlgorithm = LoggerMessage.Define<string>(
                eventId: new EventId(10, "UsingManagedKeyedHashAlgorithm"),
                logLevel: LogLevel.Debug,
                formatString: "Using managed keyed hash algorithm '{FullName}'.");
            _usingManagedSymmetricAlgorithm = LoggerMessage.Define<string>(
                eventId: new EventId(11, "UsingManagedSymmetricAlgorithm"),
                logLevel: LogLevel.Debug,
                formatString: "Using managed symmetric algorithm '{FullName}'.");
            _keyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed = LoggerMessage.Define<Guid, string>(
                eventId: new EventId(12, "KeyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed"),
                logLevel: LogLevel.Warning,
                formatString: "Key {KeyId:B} is ineligible to be the default key because its {MethodName} method failed.");
            _consideringKeyWithExpirationDateAsDefaultKey = LoggerMessage.Define<Guid, DateTimeOffset>(
                eventId: new EventId(13, "ConsideringKeyWithExpirationDateAsDefaultKey"),
                logLevel: LogLevel.Debug,
                formatString: "Considering key {KeyId:B} with expiration date {ExpirationDate:u} as default key.");
            _keyIsNoLongerUnderConsiderationAsDefault = LoggerMessage.Define<Guid>(
                eventId: new EventId(14, "KeyIsNoLongerUnderConsiderationAsDefault"),
                logLevel: LogLevel.Debug,
                formatString: "Key {KeyId:B} is no longer under consideration as default key because it is expired, revoked, or cannot be deciphered.");
            _unknownElementWithNameFoundInKeyringSkipping = LoggerMessage.Define<XName>(
                eventId: new EventId(15, "UnknownElementWithNameFoundInKeyringSkipping"),
                logLevel: LogLevel.Warning,
                formatString: "Unknown element with name '{Name}' found in keyring, skipping.");
            _markedKeyAsRevokedInTheKeyring = LoggerMessage.Define<Guid>(
                eventId: new EventId(16, "MarkedKeyAsRevokedInTheKeyring"),
                logLevel: LogLevel.Debug,
                formatString: "Marked key {KeyId:B} as revoked in the keyring.");
            _triedToProcessRevocationOfKeyButNoSuchKeyWasFound = LoggerMessage.Define<Guid>(
                eventId: new EventId(17, "TriedToProcessRevocationOfKeyButNoSuchKeyWasFound"),
                logLevel: LogLevel.Warning,
                formatString: "Tried to process revocation of key {KeyId:B}, but no such key was found in keyring. Skipping.");
            _foundKey = LoggerMessage.Define<Guid>(
                eventId: new EventId(18, "FoundKey"),
                logLevel: LogLevel.Debug,
                formatString: "Found key {KeyId:B}.");
            _foundRevocationOfAllKeysCreatedPriorTo = LoggerMessage.Define<DateTimeOffset>(
                eventId: new EventId(19, "FoundRevocationOfAllKeysCreatedPriorTo"),
                logLevel: LogLevel.Debug,
                formatString: "Found revocation of all keys created prior to {RevocationDate:u}.");
            _foundRevocationOfKey = LoggerMessage.Define<Guid>(
                eventId: new EventId(20, "FoundRevocationOfKey"),
                logLevel: LogLevel.Debug,
                formatString: "Found revocation of key {KeyId:B}.");
            _exceptionWhileProcessingRevocationElement = LoggerMessage.Define<XElement>(
                eventId: new EventId(21, "ExceptionWhileProcessingRevocationElement"),
                logLevel: LogLevel.Error,
                formatString: "An exception occurred while processing the revocation element '{RevocationElement}'. Cannot continue keyring processing.");
            _revokingAllKeysAsOfForReason = LoggerMessage.Define<DateTimeOffset, string>(
                eventId: new EventId(22, "RevokingAllKeysAsOfForReason"),
                logLevel: LogLevel.Information,
                formatString: "Revoking all keys as of {RevocationDate:u} for reason '{Reason}'.");
            _keyCacheExpirationTokenTriggeredByOperation = LoggerMessage.Define<string>(
                eventId: new EventId(23, "KeyCacheExpirationTokenTriggeredByOperation"),
                logLevel: LogLevel.Debug,
                formatString: "Key cache expiration token triggered by '{OperationName}' operation.");
            _anExceptionOccurredWhileProcessingTheKeyElement = LoggerMessage.Define<XElement>(
                eventId: new EventId(24, "ExceptionOccurredWhileProcessingTheKeyElement"),
                logLevel: LogLevel.Error,
                formatString: "An exception occurred while processing the key element '{Element}'.");
            _anExceptionOccurredWhileProcessingTheKeyElementDebug = LoggerMessage.Define<XElement>(
                eventId: new EventId(25, "ExceptionOccurredWhileProcessingTheKeyElementDebug"),
                logLevel: LogLevel.Trace,
                formatString: "An exception occurred while processing the key element '{Element}'.");
            _encryptingToWindowsDPAPIForCurrentUserAccount = LoggerMessage.Define<string>(
                eventId: new EventId(26, "EncryptingToWindowsDPAPIForCurrentUserAccount"),
                logLevel: LogLevel.Debug,
                formatString: "Encrypting to Windows DPAPI for current user account ({Name}).");
            _encryptingToWindowsDPAPINGUsingProtectionDescriptorRule = LoggerMessage.Define<string>(
                eventId: new EventId(27, "EncryptingToWindowsDPAPINGUsingProtectionDescriptorRule"),
                logLevel: LogLevel.Debug,
                formatString: "Encrypting to Windows DPAPI-NG using protection descriptor rule '{DescriptorRule}'.");
            _anErrorOccurredWhileEncryptingToX509CertificateWithThumbprint = LoggerMessage.Define<string>(
                eventId: new EventId(28, "ErrorOccurredWhileEncryptingToX509CertificateWithThumbprint"),
                logLevel: LogLevel.Error,
                formatString: "An error occurred while encrypting to X.509 certificate with thumbprint '{Thumbprint}'.");
            _encryptingToX509CertificateWithThumbprint = LoggerMessage.Define<string>(
                eventId: new EventId(29, "EncryptingToX509CertificateWithThumbprint"),
                logLevel: LogLevel.Debug,
                formatString: "Encrypting to X.509 certificate with thumbprint '{Thumbprint}'.");
            _exceptionOccurredWhileTryingToResolveCertificateWithThumbprint = LoggerMessage.Define<string>(
                eventId: new EventId(30, "ExceptionOccurredWhileTryingToResolveCertificateWithThumbprint"),
                logLevel: LogLevel.Error,
                formatString: "An exception occurred while trying to resolve certificate with thumbprint '{Thumbprint}'.");
            _performingProtectOperationToKeyWithPurposes = LoggerMessage.Define<Guid, string>(
                eventId: new EventId(31, "PerformingProtectOperationToKeyWithPurposes"),
                logLevel: LogLevel.Trace,
                formatString: "Performing protect operation to key {KeyId:B} with purposes {Purposes}.");
            _descriptorDeserializerTypeForKeyIs = LoggerMessage.Define<Guid, string>(
                eventId: new EventId(32, "DescriptorDeserializerTypeForKeyIs"),
                logLevel: LogLevel.Debug,
                formatString: "Descriptor deserializer type for key {KeyId:B} is '{AssemblyQualifiedName}'.");
            _keyEscrowSinkFoundWritingKeyToEscrow = LoggerMessage.Define<Guid>(
                eventId: new EventId(33, "KeyEscrowSinkFoundWritingKeyToEscrow"),
                logLevel: LogLevel.Debug,
                formatString: "Key escrow sink found. Writing key {KeyId:B} to escrow.");
            _noKeyEscrowSinkFoundNotWritingKeyToEscrow = LoggerMessage.Define<Guid>(
                eventId: new EventId(34, "NoKeyEscrowSinkFoundNotWritingKeyToEscrow"),
                logLevel: LogLevel.Debug,
                formatString: "No key escrow sink found. Not writing key {KeyId:B} to escrow.");
            _noXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm = LoggerMessage.Define<Guid>(
                eventId: new EventId(35, "NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm"),
                logLevel: LogLevel.Warning,
                formatString: "No XML encryptor configured. Key {KeyId:B} may be persisted to storage in unencrypted form.");
            _revokingKeyForReason = LoggerMessage.Define<Guid, DateTimeOffset, string>(
                eventId: new EventId(36, "RevokingKeyForReason"),
                logLevel: LogLevel.Information,
                formatString: "Revoking key {KeyId:B} at {RevocationDate:u} for reason '{Reason}'.");
            _readingDataFromFile = LoggerMessage.Define<string>(
                eventId: new EventId(37, "ReadingDataFromFile"),
                logLevel: LogLevel.Debug,
                formatString: "Reading data from file '{FullPath}'.");
            _nameIsNotSafeFileName = LoggerMessage.Define<string, string>(
                eventId: new EventId(38, "NameIsNotSafeFileName"),
                logLevel: LogLevel.Debug,
                formatString: "The name '{FriendlyName}' is not a safe file name, using '{NewFriendlyName}' instead.");
            _writingDataToFile = LoggerMessage.Define<string>(
                eventId: new EventId(39, "WritingDataToFile"),
                logLevel: LogLevel.Information,
                formatString: "Writing data to file '{FileName}'.");
            _readingDataFromRegistryKeyValue = LoggerMessage.Define<RegistryKey, string>(
                eventId: new EventId(40, "ReadingDataFromRegistryKeyValue"),
                logLevel: LogLevel.Debug,
                formatString: "Reading data from registry key '{RegistryKeyName}', value '{Value}'.");
            _nameIsNotSafeRegistryValueName = LoggerMessage.Define<string, string>(
                eventId: new EventId(41, "NameIsNotSafeRegistryValueName"),
                logLevel: LogLevel.Debug,
                formatString: "The name '{FriendlyName}' is not a safe registry value name, using '{NewFriendlyName}' instead.");
            _decryptingSecretElementUsingWindowsDPAPING = LoggerMessage.Define<string>(
                eventId: new EventId(42, "DecryptingSecretElementUsingWindowsDPAPING"),
                logLevel: LogLevel.Debug,
                formatString: "Decrypting secret element using Windows DPAPI-NG with protection descriptor rule '{DescriptorRule}'.");
            _exceptionOccurredTryingToDecryptElement = LoggerMessage.Define(
                eventId: new EventId(43, "ExceptionOccurredTryingToDecryptElement"),
                logLevel: LogLevel.Error,
                formatString: "An exception occurred while trying to decrypt the element.");
            _encryptingUsingNullEncryptor = LoggerMessage.Define(
                eventId: new EventId(44, "EncryptingUsingNullEncryptor"),
                logLevel: LogLevel.Warning,
                formatString: "Encrypting using a null encryptor; secret information isn't being protected.");
            _usingEphemeralDataProtectionProvider = LoggerMessage.Define(
                eventId: new EventId(45, "UsingEphemeralDataProtectionProvider"),
                logLevel: LogLevel.Warning,
                formatString: "Using ephemeral data protection provider. Payloads will be undecipherable upon application shutdown.");
            _existingCachedKeyRingIsExpiredRefreshing = LoggerMessage.Define(
                eventId: new EventId(46, "ExistingCachedKeyRingIsExpiredRefreshing"),
                logLevel: LogLevel.Debug,
                formatString: "Existing cached key ring is expired. Refreshing.");
            _errorOccurredWhileRefreshingKeyRing = LoggerMessage.Define(
                eventId: new EventId(47, "ErrorOccurredWhileRefreshingKeyRing"),
                logLevel: LogLevel.Error,
                formatString: "An error occurred while refreshing the key ring. Will try again in 2 minutes.");
            _errorOccurredWhileReadingKeyRing = LoggerMessage.Define(
                eventId: new EventId(48, "ErrorOccurredWhileReadingKeyRing"),
                logLevel: LogLevel.Error,
                formatString: "An error occurred while reading the key ring.");
            _keyRingDoesNotContainValidDefaultKey = LoggerMessage.Define(
                eventId: new EventId(49, "KeyRingDoesNotContainValidDefaultKey"),
                logLevel: LogLevel.Error,
                formatString: "The key ring does not contain a valid default key, and the key manager is configured with auto-generation of keys disabled.");
            _usingInmemoryRepository = LoggerMessage.Define(
                eventId: new EventId(50, "UsingInMemoryRepository"),
                logLevel: LogLevel.Warning,
                formatString: "Using an in-memory repository. Keys will not be persisted to storage.");
            _decryptingSecretElementUsingWindowsDPAPI = LoggerMessage.Define(
                eventId: new EventId(51, "DecryptingSecretElementUsingWindowsDPAPI"),
                logLevel: LogLevel.Debug,
                formatString: "Decrypting secret element using Windows DPAPI.");
            _defaultKeyExpirationImminentAndRepository = LoggerMessage.Define(
                eventId: new EventId(52, "DefaultKeyExpirationImminentAndRepository"),
                logLevel: LogLevel.Debug,
                formatString: "Default key expiration imminent and repository contains no viable successor. Caller should generate a successor.");
            _repositoryContainsNoViableDefaultKey = LoggerMessage.Define(
                eventId: new EventId(53, "RepositoryContainsNoViableDefaultKey"),
                logLevel: LogLevel.Debug,
                formatString: "Repository contains no viable default key. Caller should generate a key with immediate activation.");
            _errorOccurredWhileEncryptingToWindowsDPAPI = LoggerMessage.Define(
                eventId: new EventId(54, "ErrorOccurredWhileEncryptingToWindowsDPAPI"),
                logLevel: LogLevel.Error,
                formatString: "An error occurred while encrypting to Windows DPAPI.");
            _encryptingToWindowsDPAPIForLocalMachineAccount = LoggerMessage.Define(
                eventId: new EventId(55, "EncryptingToWindowsDPAPIForLocalMachineAccount"),
                logLevel: LogLevel.Debug,
                formatString: "Encrypting to Windows DPAPI for local machine account.");
            _errorOccurredWhileEncryptingToWindowsDPAPING = LoggerMessage.Define(
                eventId: new EventId(56, "ErrorOccurredWhileEncryptingToWindowsDPAPING"),
                logLevel: LogLevel.Error,
                formatString: "An error occurred while encrypting to Windows DPAPI-NG.");
            _policyResolutionStatesThatANewKeyShouldBeAddedToTheKeyRing = LoggerMessage.Define(
                eventId: new EventId(57, "PolicyResolutionStatesThatANewKeyShouldBeAddedToTheKeyRing"),
                logLevel: LogLevel.Debug,
                formatString: "Policy resolution states that a new key should be added to the key ring.");
            _creatingKey = LoggerMessage.Define<Guid, DateTimeOffset, DateTimeOffset, DateTimeOffset>(
                eventId: new EventId(58, "CreatingKey"),
                logLevel: LogLevel.Information,
                formatString: "Creating key {KeyId:B} with creation date {CreationDate:u}, activation date {ActivationDate:u}, and expiration date {ExpirationDate:u}.");
            _usingEphemeralKeyRepository = LoggerMessage.Define(
                eventId: new EventId(59, "UsingEphemeralKeyRepository"),
                logLevel: LogLevel.Warning,
                formatString: "Neither user profile nor HKLM registry available. Using an ephemeral key repository. Protected data will be unavailable when application exits.");
            _usingEphemeralFileSystemLocationInContainer = LoggerMessage.Define<string>(
                eventId: new EventId(60, "UsingEphemeralFileSystemLocationInContainer"),
                logLevel: LogLevel.Warning,
                formatString: Resources.FileSystem_EphemeralKeysLocationInContainer);

            _usingRegistryAsKeyRepositoryWithDPAPI = LoggerMessage.Define<string>(
                eventId: new EventId(61, "UsingRegistryAsKeyRepositoryWithDPAPI"),
                logLevel: LogLevel.Information,
                formatString: "User profile not available. Using '{Name}' as key repository and Windows DPAPI to encrypt keys at rest.");
            _usingProfileAsKeyRepository = LoggerMessage.Define<string>(
                eventId: new EventId(62, "UsingProfileAsKeyRepository"),
                logLevel: LogLevel.Information,
                formatString: "User profile is available. Using '{FullName}' as key repository; keys will not be encrypted at rest.");
            _usingProfileAsKeyRepositoryWithDPAPI = LoggerMessage.Define<string>(
                eventId: new EventId(63, "UsingProfileAsKeyRepositoryWithDPAPI"),
                logLevel: LogLevel.Information,
                formatString: "User profile is available. Using '{FullName}' as key repository and Windows DPAPI to encrypt keys at rest.");
            _usingAzureAsKeyRepository = LoggerMessage.Define<string>(
                eventId: new EventId(64, "UsingAzureAsKeyRepository"),
                logLevel: LogLevel.Information,
                formatString: "Azure Web Sites environment detected. Using '{FullName}' as key repository; keys will not be encrypted at rest.");
            _keyRingWasLoadedOnStartup = LoggerMessage.Define<Guid>(
                eventId: new EventId(65, "KeyRingWasLoadedOnStartup"),
                logLevel: LogLevel.Debug,
                formatString: "Key ring with default key {KeyId:B} was loaded during application startup.");
            _keyRingFailedToLoadOnStartup = LoggerMessage.Define(
                eventId: new EventId(66, "KeyRingFailedToLoadOnStartup"),
                logLevel: LogLevel.Information,
                formatString: "Key ring failed to load during application startup.");
        }

        /// <summary>
        /// Returns a value stating whether the 'debug' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDebugLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Debug);
        }

        /// <summary>
        /// Returns a value stating whether the 'error' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsErrorLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Error);
        }

        /// <summary>
        /// Returns a value stating whether the 'information' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInformationLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Information);
        }

        /// <summary>
        /// Returns a value stating whether the 'trace' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTraceLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Trace);
        }

        /// <summary>
        /// Returns a value stating whether the 'warning' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWarningLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Warning);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLogLevelEnabledCore(ILogger logger, LogLevel level)
        {
            return (logger != null && logger.IsEnabled(level));
        }

        public static void UsingFallbackKeyWithExpirationAsDefaultKey(this ILogger logger, Guid keyId, DateTimeOffset expirationDate)
        {
            _usingFallbackKeyWithExpirationAsDefaultKey(logger, keyId, expirationDate, null);
        }

        public static void UsingKeyAsDefaultKey(this ILogger logger, Guid keyId)
        {
            _usingKeyAsDefaultKey(logger, keyId, null);
        }

        public static void OpeningCNGAlgorithmFromProviderWithHMAC(this ILogger logger, string hashAlgorithm, string hashAlgorithmProvider)
        {
            _openingCNGAlgorithmFromProviderWithHMAC(logger, hashAlgorithm, hashAlgorithmProvider, null);
        }

        public static void OpeningCNGAlgorithmFromProviderWithChainingModeCBC(this ILogger logger, string encryptionAlgorithm, string encryptionAlgorithmProvider)
        {
            _openingCNGAlgorithmFromProviderWithChainingModeCBC(logger, encryptionAlgorithm, encryptionAlgorithmProvider, null);
        }

        public static void PerformingUnprotectOperationToKeyWithPurposes(this ILogger logger, Guid keyIdFromPayload, string p0)
        {
            _performingUnprotectOperationToKeyWithPurposes(logger, keyIdFromPayload, p0, null);
        }

        public static void KeyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed(this ILogger logger, Guid keyIdFromPayload)
        {
            _keyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed(logger, keyIdFromPayload, null);
        }

        public static void KeyWasRevokedCallerRequestedUnprotectOperationProceedRegardless(this ILogger logger, Guid keyIdFromPayload)
        {
            _keyWasRevokedCallerRequestedUnprotectOperationProceedRegardless(logger, keyIdFromPayload, null);
        }

        public static void KeyWasRevokedUnprotectOperationCannotProceed(this ILogger logger, Guid keyIdFromPayload)
        {
            _keyWasRevokedUnprotectOperationCannotProceed(logger, keyIdFromPayload, null);
        }

        public static void OpeningCNGAlgorithmFromProviderWithChainingModeGCM(this ILogger logger, string encryptionAlgorithm, string encryptionAlgorithmProvider)
        {
            _openingCNGAlgorithmFromProviderWithChainingModeGCM(logger, encryptionAlgorithm, encryptionAlgorithmProvider, null);
        }

        public static void UsingManagedKeyedHashAlgorithm(this ILogger logger, string fullName)
        {
            _usingManagedKeyedHashAlgorithm(logger, fullName, null);
        }

        public static void UsingManagedSymmetricAlgorithm(this ILogger logger, string fullName)
        {
            _usingManagedSymmetricAlgorithm(logger, fullName, null);
        }

        public static void KeyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed(this ILogger logger, Guid keyId, string p0, Exception exception)
        {
            _keyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed(logger, keyId, p0, exception);
        }

        public static void ConsideringKeyWithExpirationDateAsDefaultKey(this ILogger logger, Guid keyId, DateTimeOffset expirationDate)
        {
            _consideringKeyWithExpirationDateAsDefaultKey(logger, keyId, expirationDate, null);
        }

        public static void KeyIsNoLongerUnderConsiderationAsDefault(this ILogger logger, Guid keyId)
        {
            _keyIsNoLongerUnderConsiderationAsDefault(logger, keyId, null);
        }

        public static void UnknownElementWithNameFoundInKeyringSkipping(this ILogger logger, XName name)
        {
            _unknownElementWithNameFoundInKeyringSkipping(logger, name, null);
        }

        public static void MarkedKeyAsRevokedInTheKeyring(this ILogger logger, Guid revokedKeyId)
        {
            _markedKeyAsRevokedInTheKeyring(logger, revokedKeyId, null);
        }

        public static void TriedToProcessRevocationOfKeyButNoSuchKeyWasFound(this ILogger logger, Guid revokedKeyId)
        {
            _triedToProcessRevocationOfKeyButNoSuchKeyWasFound(logger, revokedKeyId, null);
        }

        public static void FoundKey(this ILogger logger, Guid keyId)
        {
            _foundKey(logger, keyId, null);
        }

        public static void FoundRevocationOfAllKeysCreatedPriorTo(this ILogger logger, DateTimeOffset massRevocationDate)
        {
            _foundRevocationOfAllKeysCreatedPriorTo(logger, massRevocationDate, null);
        }

        public static void FoundRevocationOfKey(this ILogger logger, Guid keyId)
        {
            _foundRevocationOfKey(logger, keyId, null);
        }

        public static void ExceptionWhileProcessingRevocationElement(this ILogger logger, XElement revocationElement, Exception exception)
        {
            _exceptionWhileProcessingRevocationElement(logger, revocationElement, exception);
        }

        public static void RevokingAllKeysAsOfForReason(this ILogger logger, DateTimeOffset revocationDate, string reason)
        {
            _revokingAllKeysAsOfForReason(logger, revocationDate, reason, null);
        }

        public static void KeyCacheExpirationTokenTriggeredByOperation(this ILogger logger, string opName)
        {
            _keyCacheExpirationTokenTriggeredByOperation(logger, opName, null);
        }

        public static void ExceptionWhileProcessingKeyElement(this ILogger logger, XElement keyElement, Exception exception)
        {
            _anExceptionOccurredWhileProcessingTheKeyElement(logger, keyElement, exception);
        }

        public static void AnExceptionOccurredWhileProcessingElementDebug(this ILogger logger, XElement keyElement, Exception exception)
        {
            _anExceptionOccurredWhileProcessingTheKeyElementDebug(logger, keyElement, exception);
        }

        public static void EncryptingToWindowsDPAPIForCurrentUserAccount(this ILogger logger, string name)
        {
            _encryptingToWindowsDPAPIForCurrentUserAccount(logger, name, null);
        }

        public static void AnErrorOccurredWhileEncryptingToX509CertificateWithThumbprint(this ILogger logger, string thumbprint, Exception exception)
        {
            _anErrorOccurredWhileEncryptingToX509CertificateWithThumbprint(logger, thumbprint, exception);
        }

        public static void EncryptingToX509CertificateWithThumbprint(this ILogger logger, string thumbprint)
        {
            _encryptingToX509CertificateWithThumbprint(logger, thumbprint, null);
        }

        public static void ExceptionWhileTryingToResolveCertificateWithThumbprint(this ILogger logger, string thumbprint, Exception exception)
        {
            _exceptionOccurredWhileTryingToResolveCertificateWithThumbprint(logger, thumbprint, exception);
        }

        public static void PerformingProtectOperationToKeyWithPurposes(this ILogger logger, Guid defaultKeyId, string p0)
        {
            _performingProtectOperationToKeyWithPurposes(logger, defaultKeyId, p0, null);
        }

        public static void DescriptorDeserializerTypeForKeyIs(this ILogger logger, Guid keyId, string assemblyQualifiedName)
        {
            _descriptorDeserializerTypeForKeyIs(logger, keyId, assemblyQualifiedName, null);
        }

        public static void KeyEscrowSinkFoundWritingKeyToEscrow(this ILogger logger, Guid keyId)
        {
            _keyEscrowSinkFoundWritingKeyToEscrow(logger, keyId, null);
        }

        public static void NoKeyEscrowSinkFoundNotWritingKeyToEscrow(this ILogger logger, Guid keyId)
        {
            _noKeyEscrowSinkFoundNotWritingKeyToEscrow(logger, keyId, null);
        }

        public static void NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm(this ILogger logger, Guid keyId)
        {
            _noXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm(logger, keyId, null);
        }

        public static void RevokingKeyForReason(this ILogger logger, Guid keyId, DateTimeOffset revocationDate, string reason)
        {
            _revokingKeyForReason(logger, keyId, revocationDate, reason, null);
        }

        public static void ReadingDataFromFile(this ILogger logger, string fullPath)
        {
            _readingDataFromFile(logger, fullPath, null);
        }

        public static void NameIsNotSafeFileName(this ILogger logger, string friendlyName, string newFriendlyName)
        {
            _nameIsNotSafeFileName(logger, friendlyName, newFriendlyName, null);
        }

        public static void WritingDataToFile(this ILogger logger, string finalFilename)
        {
            _writingDataToFile(logger, finalFilename, null);
        }

        public static void ReadingDataFromRegistryKeyValue(this ILogger logger, RegistryKey regKey, string valueName)
        {
            _readingDataFromRegistryKeyValue(logger, regKey, valueName, null);
        }

        public static void NameIsNotSafeRegistryValueName(this ILogger logger, string friendlyName, string newFriendlyName)
        {
            _nameIsNotSafeRegistryValueName(logger, friendlyName, newFriendlyName, null);
        }

        public static void DecryptingSecretElementUsingWindowsDPAPING(this ILogger logger, string protectionDescriptorRule)
        {
            _decryptingSecretElementUsingWindowsDPAPING(logger, protectionDescriptorRule, null);
        }

        public static void EncryptingToWindowsDPAPINGUsingProtectionDescriptorRule(this ILogger logger, string protectionDescriptorRuleString)
        {
            _encryptingToWindowsDPAPINGUsingProtectionDescriptorRule(logger, protectionDescriptorRuleString, null);
        }

        public static void ExceptionOccurredTryingToDecryptElement(this ILogger logger, Exception exception)
        {
            _exceptionOccurredTryingToDecryptElement(logger, exception);
        }

        public static void EncryptingUsingNullEncryptor(this ILogger logger)
        {
            _encryptingUsingNullEncryptor(logger, null);
        }

        public static void UsingEphemeralDataProtectionProvider(this ILogger logger)
        {
            _usingEphemeralDataProtectionProvider(logger, null);
        }

        public static void ExistingCachedKeyRingIsExpired(this ILogger logger)
        {
            _existingCachedKeyRingIsExpiredRefreshing(logger, null);
        }

        public static void ErrorOccurredWhileRefreshingKeyRing(this ILogger logger, Exception exception)
        {
            _errorOccurredWhileRefreshingKeyRing(logger, exception);
        }

        public static void ErrorOccurredWhileReadingKeyRing(this ILogger logger, Exception exception)
        {
            _errorOccurredWhileReadingKeyRing(logger, exception);
        }

        public static void KeyRingDoesNotContainValidDefaultKey(this ILogger logger)
        {
            _keyRingDoesNotContainValidDefaultKey(logger, null);
        }

        public static void UsingInmemoryRepository(this ILogger logger)
        {
            _usingInmemoryRepository(logger, null);
        }

        public static void DecryptingSecretElementUsingWindowsDPAPI(this ILogger logger)
        {
            _decryptingSecretElementUsingWindowsDPAPI(logger, null);
        }

        public static void DefaultKeyExpirationImminentAndRepository(this ILogger logger)
        {
            _defaultKeyExpirationImminentAndRepository(logger, null);
        }

        public static void RepositoryContainsNoViableDefaultKey(this ILogger logger)
        {
            _repositoryContainsNoViableDefaultKey(logger, null);
        }

        public static void ErrorOccurredWhileEncryptingToWindowsDPAPI(this ILogger logger, Exception exception)
        {
            _errorOccurredWhileEncryptingToWindowsDPAPI(logger, exception);
        }

        public static void EncryptingToWindowsDPAPIForLocalMachineAccount(this ILogger logger)
        {
            _encryptingToWindowsDPAPIForLocalMachineAccount(logger, null);
        }

        public static void ErrorOccurredWhileEncryptingToWindowsDPAPING(this ILogger logger, Exception exception)
        {
            _errorOccurredWhileEncryptingToWindowsDPAPING(logger, exception);
        }

        public static void PolicyResolutionStatesThatANewKeyShouldBeAddedToTheKeyRing(this ILogger logger)
        {
            _policyResolutionStatesThatANewKeyShouldBeAddedToTheKeyRing(logger, null);
        }

        public static void CreatingKey(this ILogger logger, Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate)
        {
            _creatingKey(logger, keyId, creationDate, activationDate, expirationDate, null);
        }

        public static void UsingEphemeralKeyRepository(this ILogger logger)
        {
            _usingEphemeralKeyRepository(logger, null);
        }

        public static void UsingRegistryAsKeyRepositoryWithDPAPI(this ILogger logger, string name)
        {
            _usingRegistryAsKeyRepositoryWithDPAPI(logger, name, null);
        }

        public static void UsingProfileAsKeyRepository(this ILogger logger, string fullName)
        {
            _usingProfileAsKeyRepository(logger, fullName, null);
        }

        public static void UsingProfileAsKeyRepositoryWithDPAPI(this ILogger logger, string fullName)
        {
            _usingProfileAsKeyRepositoryWithDPAPI(logger, fullName, null);
        }

        public static void UsingAzureAsKeyRepository(this ILogger logger, string fullName)
        {
            _usingAzureAsKeyRepository(logger, fullName, null);
        }

        public static void KeyRingWasLoadedOnStartup(this ILogger logger, Guid defaultKeyId)
        {
            _keyRingWasLoadedOnStartup(logger, defaultKeyId, null);
        }

        public static void KeyRingFailedToLoadOnStartup(this ILogger logger, Exception innerException)
        {
            _keyRingFailedToLoadOnStartup(logger, innerException);
        }

        public static void UsingEphemeralFileSystemLocationInContainer(this ILogger logger, string path)
        {
            _usingEphemeralFileSystemLocationInContainer(logger, path, null);
        }
    }
}
