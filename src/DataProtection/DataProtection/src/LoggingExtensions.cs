// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Win32;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Helpful extension methods on <see cref="ILogger"/>.
/// </summary>
internal static partial class LoggingExtensions
{
    /// <summary>
    /// Returns a value stating whether the 'debug' log level is enabled.
    /// Returns false if the logger instance is null.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDebugLevelEnabled([NotNullWhen(true)] this ILogger? logger)
    {
        return IsLogLevelEnabledCore(logger, LogLevel.Debug);
    }

    /// <summary>
    /// Returns a value stating whether the 'trace' log level is enabled.
    /// Returns false if the logger instance is null.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTraceLevelEnabled([NotNullWhen(true)] this ILogger? logger)
    {
        return IsLogLevelEnabledCore(logger, LogLevel.Trace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLogLevelEnabledCore([NotNullWhen(true)] ILogger? logger, LogLevel level)
    {
        return (logger != null && logger.IsEnabled(level));
    }

    [LoggerMessage(1, LogLevel.Warning, "Policy resolution states that a new key should be added to the key ring, but automatic generation of keys is disabled. Using fallback key {KeyId:B} with expiration {ExpirationDate:u} as default key.", EventName = "UsingFallbackKeyWithExpirationAsDefaultKey")]
    public static partial void UsingFallbackKeyWithExpirationAsDefaultKey(this ILogger logger, Guid keyId, DateTimeOffset expirationDate);

    [LoggerMessage(2, LogLevel.Debug, "Using key {KeyId:B} as the default key.", EventName = "UsingKeyAsDefaultKey")]
    public static partial void UsingKeyAsDefaultKey(this ILogger logger, Guid keyId);

    [LoggerMessage(3, LogLevel.Debug, "Opening CNG algorithm '{HashAlgorithm}' from provider '{HashAlgorithmProvider}' with HMAC.", EventName = "OpeningCNGAlgorithmFromProviderWithHMAC")]
    public static partial void OpeningCNGAlgorithmFromProviderWithHMAC(this ILogger logger, string hashAlgorithm, string? hashAlgorithmProvider);

    [LoggerMessage(4, LogLevel.Debug, "Opening CNG algorithm '{EncryptionAlgorithm}' from provider '{EncryptionAlgorithmProvider}' with chaining mode CBC.", EventName = "OpeningCNGAlgorithmFromProviderWithChainingModeCBC")]
    public static partial void OpeningCNGAlgorithmFromProviderWithChainingModeCBC(this ILogger logger, string encryptionAlgorithm, string? encryptionAlgorithmProvider);

    [LoggerMessage(5, LogLevel.Trace, "Performing unprotect operation to key {KeyId:B} with purposes {Purposes}.", EventName = "PerformingUnprotectOperationToKeyWithPurposes")]
    public static partial void PerformingUnprotectOperationToKeyWithPurposes(this ILogger logger, Guid keyId, string purposes);

    [LoggerMessage(6, LogLevel.Trace, "Key {KeyId:B} was not found in the key ring. Unprotect operation cannot proceed.", EventName = "KeyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed")]
    public static partial void KeyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed(this ILogger logger, Guid keyId);

    [LoggerMessage(7, LogLevel.Debug, "Key {KeyId:B} was revoked. Caller requested unprotect operation proceed regardless.", EventName = "KeyWasRevokedCallerRequestedUnprotectOperationProceedRegardless")]
    public static partial void KeyWasRevokedCallerRequestedUnprotectOperationProceedRegardless(this ILogger logger, Guid keyId);

    [LoggerMessage(8, LogLevel.Debug, "Key {KeyId:B} was revoked. Unprotect operation cannot proceed.", EventName = "KeyWasRevokedUnprotectOperationCannotProceed")]
    public static partial void KeyWasRevokedUnprotectOperationCannotProceed(this ILogger logger, Guid keyId);

    [LoggerMessage(9, LogLevel.Debug, "Opening CNG algorithm '{EncryptionAlgorithm}' from provider '{EncryptionAlgorithmProvider}' with chaining mode GCM.", EventName = "OpeningCNGAlgorithmFromProviderWithChainingModeGCM")]
    public static partial void OpeningCNGAlgorithmFromProviderWithChainingModeGCM(this ILogger logger, string encryptionAlgorithm, string? encryptionAlgorithmProvider);

    [LoggerMessage(10, LogLevel.Debug, "Using managed keyed hash algorithm '{FullName}'.", EventName = "UsingManagedKeyedHashAlgorithm")]
    public static partial void UsingManagedKeyedHashAlgorithm(this ILogger logger, string fullName);

    [LoggerMessage(11, LogLevel.Debug, "Using managed symmetric algorithm '{FullName}'.", EventName = "UsingManagedSymmetricAlgorithm")]
    public static partial void UsingManagedSymmetricAlgorithm(this ILogger logger, string fullName);

    [LoggerMessage(12, LogLevel.Warning, "Key {KeyId:B} is ineligible to be the default key because its {MethodName} method failed after the maximum number of retries.", EventName = "KeyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed")]
    public static partial void KeyIsIneligibleToBeTheDefaultKeyBecauseItsMethodFailed(this ILogger logger, Guid keyId, string methodName, Exception exception);

    [LoggerMessage(13, LogLevel.Debug, "Considering key {KeyId:B} with expiration date {ExpirationDate:u} as default key.", EventName = "ConsideringKeyWithExpirationDateAsDefaultKey")]
    public static partial void ConsideringKeyWithExpirationDateAsDefaultKey(this ILogger logger, Guid keyId, DateTimeOffset expirationDate);

    [LoggerMessage(14, LogLevel.Debug, "Key {KeyId:B} is no longer under consideration as default key because it is expired, revoked, or cannot be deciphered.", EventName = "KeyIsNoLongerUnderConsiderationAsDefault")]
    public static partial void KeyIsNoLongerUnderConsiderationAsDefault(this ILogger logger, Guid keyId);

    [LoggerMessage(15, LogLevel.Warning, "Unknown element with name '{Name}' found in keyring, skipping.", EventName = "UnknownElementWithNameFoundInKeyringSkipping")]
    public static partial void UnknownElementWithNameFoundInKeyringSkipping(this ILogger logger, XName name);

    [LoggerMessage(16, LogLevel.Debug, "Marked key {KeyId:B} as revoked in the keyring.", EventName = "MarkedKeyAsRevokedInTheKeyring")]
    public static partial void MarkedKeyAsRevokedInTheKeyring(this ILogger logger, Guid keyId);

    [LoggerMessage(17, LogLevel.Warning, "Tried to process revocation of key {KeyId:B}, but no such key was found in keyring. Skipping.", EventName = "TriedToProcessRevocationOfKeyButNoSuchKeyWasFound")]
    public static partial void TriedToProcessRevocationOfKeyButNoSuchKeyWasFound(this ILogger logger, Guid keyId);

    [LoggerMessage(18, LogLevel.Debug, "Found key {KeyId:B}.", EventName = "FoundKey")]
    public static partial void FoundKey(this ILogger logger, Guid keyId);

    [LoggerMessage(19, LogLevel.Debug, "Found revocation of all keys created prior to {RevocationDate:u}.", EventName = "FoundRevocationOfAllKeysCreatedPriorTo")]
    public static partial void FoundRevocationOfAllKeysCreatedPriorTo(this ILogger logger, DateTimeOffset revocationDate);

    [LoggerMessage(20, LogLevel.Debug, "Found revocation of key {KeyId:B}.", EventName = "FoundRevocationOfKey")]
    public static partial void FoundRevocationOfKey(this ILogger logger, Guid keyId);

    [LoggerMessage(21, LogLevel.Error, "An exception occurred while processing the revocation element '{RevocationElement}'. Cannot continue keyring processing.", EventName = "ExceptionWhileProcessingRevocationElement")]
    public static partial void ExceptionWhileProcessingRevocationElement(this ILogger logger, XElement revocationElement, Exception exception);

    [LoggerMessage(22, LogLevel.Information, "Revoking all keys as of {RevocationDate:u} for reason '{Reason}'.", EventName = "RevokingAllKeysAsOfForReason")]
    public static partial void RevokingAllKeysAsOfForReason(this ILogger logger, DateTimeOffset revocationDate, string? reason);

    [LoggerMessage(23, LogLevel.Debug, "Key cache expiration token triggered by '{OperationName}' operation.", EventName = "KeyCacheExpirationTokenTriggeredByOperation")]
    public static partial void KeyCacheExpirationTokenTriggeredByOperation(this ILogger logger, string operationName);

    [LoggerMessage(24, LogLevel.Error, "An exception occurred while processing the key element '{Element}'.", EventName = "ExceptionOccurredWhileProcessingTheKeyElement")]
    public static partial void ExceptionWhileProcessingKeyElement(this ILogger logger, XElement element, Exception exception);

    [LoggerMessage(25, LogLevel.Trace, "An exception occurred while processing the key element '{Element}'.", EventName = "ExceptionOccurredWhileProcessingTheKeyElementDebug")]
    public static partial void AnExceptionOccurredWhileProcessingElementDebug(this ILogger logger, XElement element, Exception exception);

    [LoggerMessage(26, LogLevel.Debug, "Encrypting to Windows DPAPI for current user account ({Name}).", EventName = "EncryptingToWindowsDPAPIForCurrentUserAccount")]
    public static partial void EncryptingToWindowsDPAPIForCurrentUserAccount(this ILogger logger, string name);

    [LoggerMessage(28, LogLevel.Error, "An error occurred while encrypting to X.509 certificate with thumbprint '{Thumbprint}'.", EventName = "ErrorOccurredWhileEncryptingToX509CertificateWithThumbprint")]
    public static partial void AnErrorOccurredWhileEncryptingToX509CertificateWithThumbprint(this ILogger logger, string thumbprint, Exception exception);

    [LoggerMessage(29, LogLevel.Debug, "Encrypting to X.509 certificate with thumbprint '{Thumbprint}'.", EventName = "EncryptingToX509CertificateWithThumbprint")]
    public static partial void EncryptingToX509CertificateWithThumbprint(this ILogger logger, string thumbprint);

    [LoggerMessage(30, LogLevel.Error, "An exception occurred while trying to resolve certificate with thumbprint '{Thumbprint}'.", EventName = "ExceptionOccurredWhileTryingToResolveCertificateWithThumbprint")]
    public static partial void ExceptionWhileTryingToResolveCertificateWithThumbprint(this ILogger logger, string thumbprint, Exception exception);

    [LoggerMessage(31, LogLevel.Trace, "Performing protect operation to key {KeyId:B} with purposes {Purposes}.", EventName = "PerformingProtectOperationToKeyWithPurposes")]
    public static partial void PerformingProtectOperationToKeyWithPurposes(this ILogger logger, Guid keyId, string purposes);

    [LoggerMessage(32, LogLevel.Debug, "Descriptor deserializer type for key {KeyId:B} is '{AssemblyQualifiedName}'.", EventName = "DescriptorDeserializerTypeForKeyIs")]
    public static partial void DescriptorDeserializerTypeForKeyIs(this ILogger logger, Guid keyId, string assemblyQualifiedName);

    [LoggerMessage(33, LogLevel.Debug, "Key escrow sink found. Writing key {KeyId:B} to escrow.", EventName = "KeyEscrowSinkFoundWritingKeyToEscrow")]
    public static partial void KeyEscrowSinkFoundWritingKeyToEscrow(this ILogger logger, Guid keyId);

    [LoggerMessage(34, LogLevel.Debug, "No key escrow sink found. Not writing key {KeyId:B} to escrow.", EventName = "NoKeyEscrowSinkFoundNotWritingKeyToEscrow")]
    public static partial void NoKeyEscrowSinkFoundNotWritingKeyToEscrow(this ILogger logger, Guid keyId);

    [LoggerMessage(35, LogLevel.Warning, "No XML encryptor configured. Key {KeyId:B} may be persisted to storage in unencrypted form.", EventName = "NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm")]
    public static partial void NoXMLEncryptorConfiguredKeyMayBePersistedToStorageInUnencryptedForm(this ILogger logger, Guid keyId);

    [LoggerMessage(36, LogLevel.Information, "Revoking key {KeyId:B} at {RevocationDate:u} for reason '{Reason}'.", EventName = "RevokingKeyForReason")]
    public static partial void RevokingKeyForReason(this ILogger logger, Guid keyId, DateTimeOffset revocationDate, string? reason);

    [LoggerMessage(37, LogLevel.Debug, "Reading data from file '{FullPath}'.", EventName = "ReadingDataFromFile")]
    public static partial void ReadingDataFromFile(this ILogger logger, string fullPath);

    [LoggerMessage(38, LogLevel.Debug, "The name '{FriendlyName}' is not a safe file name, using '{NewFriendlyName}' instead.", EventName = "NameIsNotSafeFileName")]
    public static partial void NameIsNotSafeFileName(this ILogger logger, string friendlyName, string newFriendlyName);

    [LoggerMessage(39, LogLevel.Information, "Writing data to file '{FileName}'.", EventName = "WritingDataToFile")]
    public static partial void WritingDataToFile(this ILogger logger, string fileName);

    [LoggerMessage(40, LogLevel.Debug, "Reading data from registry key '{RegistryKeyName}', value '{Value}'.", EventName = "ReadingDataFromRegistryKeyValue")]
    public static partial void ReadingDataFromRegistryKeyValue(this ILogger logger, RegistryKey registryKeyName, string value);

    [LoggerMessage(41, LogLevel.Debug, "The name '{FriendlyName}' is not a safe registry value name, using '{NewFriendlyName}' instead.", EventName = "NameIsNotSafeRegistryValueName")]
    public static partial void NameIsNotSafeRegistryValueName(this ILogger logger, string friendlyName, string newFriendlyName);

    [LoggerMessage(42, LogLevel.Debug, "Decrypting secret element using Windows DPAPI-NG with protection descriptor rule '{DescriptorRule}'.", EventName = "DecryptingSecretElementUsingWindowsDPAPING")]
    public static partial void DecryptingSecretElementUsingWindowsDPAPING(this ILogger logger, string? descriptorRule);

    [LoggerMessage(27, LogLevel.Debug, "Encrypting to Windows DPAPI-NG using protection descriptor rule '{DescriptorRule}'.", EventName = "EncryptingToWindowsDPAPINGUsingProtectionDescriptorRule")]
    public static partial void EncryptingToWindowsDPAPINGUsingProtectionDescriptorRule(this ILogger logger, string descriptorRule);

    [LoggerMessage(43, LogLevel.Error, "An exception occurred while trying to decrypt the element.", EventName = "ExceptionOccurredTryingToDecryptElement")]
    public static partial void ExceptionOccurredTryingToDecryptElement(this ILogger logger, Exception exception);

    [LoggerMessage(44, LogLevel.Warning, "Encrypting using a null encryptor; secret information isn't being protected.", EventName = "EncryptingUsingNullEncryptor")]
    public static partial void EncryptingUsingNullEncryptor(this ILogger logger);

    [LoggerMessage(45, LogLevel.Information, "Using ephemeral data protection provider. Payloads will be undecipherable upon application shutdown.", EventName = "UsingEphemeralDataProtectionProvider")]
    public static partial void UsingEphemeralDataProtectionProvider(this ILogger logger);

    [LoggerMessage(46, LogLevel.Debug, "Existing cached key ring is expired. Refreshing.", EventName = "ExistingCachedKeyRingIsExpiredRefreshing")]
    public static partial void ExistingCachedKeyRingIsExpired(this ILogger logger);

    [LoggerMessage(47, LogLevel.Error, "An error occurred while refreshing the key ring. Will try again in 2 minutes.", EventName = "ErrorOccurredWhileRefreshingKeyRing")]
    public static partial void ErrorOccurredWhileRefreshingKeyRing(this ILogger logger, Exception exception);

    [LoggerMessage(48, LogLevel.Error, "An error occurred while reading the key ring.", EventName = "ErrorOccurredWhileReadingKeyRing")]
    public static partial void ErrorOccurredWhileReadingKeyRing(this ILogger logger, Exception exception);

    [LoggerMessage(49, LogLevel.Error, "The key ring does not contain a valid default protection key. The data protection system cannot create a new key because auto-generation of keys is disabled. For more information go to http://aka.ms/dataprotectionwarning", EventName = "KeyRingDoesNotContainValidDefaultKey")]
    public static partial void KeyRingDoesNotContainValidDefaultKey(this ILogger logger);

    [LoggerMessage(50, LogLevel.Warning, "Using an in-memory repository. Keys will not be persisted to storage.", EventName = "UsingInMemoryRepository")]
    public static partial void UsingInmemoryRepository(this ILogger logger);

    [LoggerMessage(51, LogLevel.Debug, "Decrypting secret element using Windows DPAPI.", EventName = "DecryptingSecretElementUsingWindowsDPAPI")]
    public static partial void DecryptingSecretElementUsingWindowsDPAPI(this ILogger logger);

    [LoggerMessage(52, LogLevel.Debug, "Default key expiration imminent and repository contains no viable successor. Caller should generate a successor.", EventName = "DefaultKeyExpirationImminentAndRepository")]
    public static partial void DefaultKeyExpirationImminentAndRepository(this ILogger logger);

    [LoggerMessage(53, LogLevel.Debug, "Repository contains no viable default key. Caller should generate a key with immediate activation.", EventName = "RepositoryContainsNoViableDefaultKey")]
    public static partial void RepositoryContainsNoViableDefaultKey(this ILogger logger);

    [LoggerMessage(54, LogLevel.Error, "An error occurred while encrypting to Windows DPAPI.", EventName = "ErrorOccurredWhileEncryptingToWindowsDPAPI")]
    public static partial void ErrorOccurredWhileEncryptingToWindowsDPAPI(this ILogger logger, Exception exception);

    [LoggerMessage(55, LogLevel.Debug, "Encrypting to Windows DPAPI for local machine account.", EventName = "EncryptingToWindowsDPAPIForLocalMachineAccount")]
    public static partial void EncryptingToWindowsDPAPIForLocalMachineAccount(this ILogger logger);

    [LoggerMessage(56, LogLevel.Error, "An error occurred while encrypting to Windows DPAPI-NG.", EventName = "ErrorOccurredWhileEncryptingToWindowsDPAPING")]
    public static partial void ErrorOccurredWhileEncryptingToWindowsDPAPING(this ILogger logger, Exception exception);

    [LoggerMessage(57, LogLevel.Debug, "Policy resolution states that a new key should be added to the key ring.", EventName = "PolicyResolutionStatesThatANewKeyShouldBeAddedToTheKeyRing")]
    public static partial void PolicyResolutionStatesThatANewKeyShouldBeAddedToTheKeyRing(this ILogger logger);

    [LoggerMessage(58, LogLevel.Information, "Creating key {KeyId:B} with creation date {CreationDate:u}, activation date {ActivationDate:u}, and expiration date {ExpirationDate:u}.", EventName = "CreatingKey")]
    public static partial void CreatingKey(this ILogger logger, Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate);

    [LoggerMessage(59, LogLevel.Warning, "Neither user profile nor HKLM registry available. Using an ephemeral key repository. Protected data will be unavailable when application exits.", EventName = "UsingEphemeralKeyRepository")]
    public static partial void UsingEphemeralKeyRepository(this ILogger logger);

    [LoggerMessage(61, LogLevel.Information, "User profile not available. Using '{Name}' as key repository and Windows DPAPI to encrypt keys at rest.", EventName = "UsingRegistryAsKeyRepositoryWithDPAPI")]
    public static partial void UsingRegistryAsKeyRepositoryWithDPAPI(this ILogger logger, string name);

    [LoggerMessage(62, LogLevel.Information, "User profile is available. Using '{FullName}' as key repository; keys will not be encrypted at rest.", EventName = "UsingProfileAsKeyRepository")]
    public static partial void UsingProfileAsKeyRepository(this ILogger logger, string fullName);

    [LoggerMessage(63, LogLevel.Information, "User profile is available. Using '{FullName}' as key repository and Windows DPAPI to encrypt keys at rest.", EventName = "UsingProfileAsKeyRepositoryWithDPAPI")]
    public static partial void UsingProfileAsKeyRepositoryWithDPAPI(this ILogger logger, string fullName);

    [LoggerMessage(64, LogLevel.Information, "Azure Web Sites environment detected. Using '{FullName}' as key repository; keys will not be encrypted at rest.", EventName = "UsingAzureAsKeyRepository")]
    public static partial void UsingAzureAsKeyRepository(this ILogger logger, string fullName);

    [LoggerMessage(65, LogLevel.Debug, "Key ring with default key {KeyId:B} was loaded during application startup.", EventName = "KeyRingWasLoadedOnStartup")]
    public static partial void KeyRingWasLoadedOnStartup(this ILogger logger, Guid keyId);

    [LoggerMessage(66, LogLevel.Information, "Key ring failed to load during application startup.", EventName = "KeyRingFailedToLoadOnStartup")]
    public static partial void KeyRingFailedToLoadOnStartup(this ILogger logger, Exception innerException);

    [LoggerMessage(60, LogLevel.Warning, "Storing keys in a directory '{path}' that may not be persisted outside of the container. Protected data will be unavailable when container is destroyed. For more information go to https://aka.ms/aspnet/dataprotectionwarning", EventName = "UsingEphemeralFileSystemLocationInContainer")]
    public static partial void UsingEphemeralFileSystemLocationInContainer(this ILogger logger, string path);

    [LoggerMessage(61, LogLevel.Trace, "Ignoring configuration '{PropertyName}' for options instance '{OptionsName}'", EventName = "IgnoringReadOnlyConfigurationForNonDefaultOptions")]
    public static partial void IgnoringReadOnlyConfigurationForNonDefaultOptions(this ILogger logger, string propertyName, string? optionsName);

    [LoggerMessage(62, LogLevel.Information, "Enabling read-only key access with repository directory '{Path}'", EventName = "UsingReadOnlyKeyConfiguration")]
    public static partial void UsingReadOnlyKeyConfiguration(this ILogger logger, string path);

    [LoggerMessage(63, LogLevel.Debug, "Not enabling read-only key access because an XML repository has been specified", EventName = "NotUsingReadOnlyKeyConfigurationBecauseOfRepository")]
    public static partial void NotUsingReadOnlyKeyConfigurationBecauseOfRepository(this ILogger logger);

    [LoggerMessage(64, LogLevel.Debug, "Not enabling read-only key access because an XML encryptor has been specified", EventName = "NotUsingReadOnlyKeyConfigurationBecauseOfEncryptor")]
    public static partial void NotUsingReadOnlyKeyConfigurationBecauseOfEncryptor(this ILogger logger);

    [LoggerMessage(72, LogLevel.Error, "The key ring's default data protection key {KeyId:B} has been revoked.", EventName = "KeyRingDefaultKeyIsRevoked")]
    public static partial void KeyRingDefaultKeyIsRevoked(this ILogger logger, Guid keyId);

    [LoggerMessage(73, LogLevel.Debug, "Key {KeyId:B} method {MethodName} failed. Retrying.", EventName = "RetryingMethodOfKeyAfterFailure")]
    public static partial void RetryingMethodOfKeyAfterFailure(this ILogger logger, Guid keyId, string methodName, Exception exception);
	
    [LoggerMessage(74, LogLevel.Debug, "Deleting file '{FileName}'.", EventName = "DeletingFile")]
    public static partial void DeletingFile(this ILogger logger, string fileName);

    [LoggerMessage(75, LogLevel.Error, "Failed to delete file '{FileName}'.  Not attempting further deletions.", EventName = "FailedToDeleteFile")]
    public static partial void FailedToDeleteFile(this ILogger logger, string fileName, Exception exception);

    [LoggerMessage(76, LogLevel.Debug, "Deleting registry key '{RegistryKeyName}', value '{Value}'.", EventName = "RemovingDataFromRegistryKeyValue")]
    public static partial void RemovingDataFromRegistryKeyValue(this ILogger logger, RegistryKey registryKeyName, string value);

    [LoggerMessage(77, LogLevel.Error, "Failed to delete registry key '{RegistryKeyName}', value '{ValueName}'.  Not attempting further deletions.", EventName = "FailedToRemoveDataFromRegistryKeyValue")]
    public static partial void FailedToRemoveDataFromRegistryKeyValue(this ILogger logger, RegistryKey registryKeyName, string valueName, Exception exception);

    [LoggerMessage(78, LogLevel.Trace, "Found multiple revocation entries for key {KeyId:B}.", EventName = "KeyRevokedMultipleTimes")]
    public static partial void KeyRevokedMultipleTimes(this ILogger logger, Guid keyId);

    [LoggerMessage(79, LogLevel.Trace, "Ignoring revocation of keys created before {OlderDate:u} in favor of revocation of keys created before {NewerDate:u}.", EventName = "DateBasedRevocationSuperseded")]
    public static partial void DateBasedRevocationSuperseded(this ILogger logger, DateTimeOffset olderDate, DateTimeOffset newerDate);

    [LoggerMessage(80, LogLevel.Debug, "Deleting key {KeyId:B}.", EventName = "DeletingKey")]
    public static partial void DeletingKey(this ILogger logger, Guid keyId);
}
