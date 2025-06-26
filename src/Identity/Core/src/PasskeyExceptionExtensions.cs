// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Identity;

internal static class PasskeyExceptionExtensions
{
    extension(PasskeyException)
    {
        public static PasskeyException InvalidCredentialType(string expectedType, string actualType)
            => new($"Expected credential type '{expectedType}', got '{actualType}'.");

        public static PasskeyException InvalidClientDataType(string expectedType, string actualType)
            => new($"Expected the client data JSON 'type' field to be '{expectedType}', got '{actualType}'.");

        public static PasskeyException InvalidChallenge()
            => new("The authenticator response challenge does not match original challenge.");

        public static PasskeyException InvalidOrigin(string origin)
            => new($"The authenticator response had an invalid origin '{origin}'.");

        public static PasskeyException InvalidRelyingPartyIDHash()
            => new("The authenticator data included an invalid Relying Party ID hash.");

        public static PasskeyException UserNotPresent()
            => new("The authenticator data flags did not include the 'UserPresent' flag.");

        public static PasskeyException UserNotVerified()
            => new("User verification is required, but the authenticator data flags did not have the 'UserVerified' flag.");

        public static PasskeyException NotBackupEligibleYetBackedUp()
            => new("The credential is backed up, but the authenticator data flags did not have the 'BackupEligible' flag.");

        public static PasskeyException BackupEligibilityDisallowedYetBackupEligible()
            => new("Credential backup eligibility is disallowed, but the credential was eligible for backup.");

        public static PasskeyException BackupEligibilityRequiredYetNotBackupEligible()
            => new("Credential backup eligibility is required, but the credential was not eligible for backup.");

        public static PasskeyException BackupDisallowedYetBackedUp()
            => new("Credential backup is disallowed, but the credential was backed up.");

        public static PasskeyException BackupRequiredYetNotBackedUp()
            => new("Credential backup is required, but the credential was not backed up.");

        public static PasskeyException MissingAttestedCredentialData()
            => new("No attested credential data was provided by the authenticator.");

        public static PasskeyException UnsupportedCredentialPublicKeyAlgorithm()
            => new("The credential public key algorithm does not match any of the supported algorithms.");

        public static PasskeyException InvalidAttestationStatement()
            => new("The attestation statement was not valid.");

        public static PasskeyException InvalidCredentialIdLength(int length)
            => new($"Expected the credential ID to have a length between 1 and 1023 bytes, but got {length}.");

        public static PasskeyException CredentialIdMismatch()
            => new("The provided credential ID does not match the credential ID in the attested credential data.");

        public static PasskeyException CredentialAlreadyRegistered()
            => new("The credential is already registered for a user.");

        public static PasskeyException CredentialNotAllowed()
            => new("The provided credential ID was not in the list of allowed credentials.");

        public static PasskeyException CredentialDoesNotBelongToUser()
            => new("The provided credential does not belong to the specified user.");

        public static PasskeyException UserHandleMismatch(string providedUserHandle, string credentialUserHandle)
            => new($"The provided user handle '{providedUserHandle}' does not match the credential's user handle '{credentialUserHandle}'.");

        public static PasskeyException MissingUserHandle()
            => new("The authenticator response was missing a user handle.");

        public static PasskeyException ExpectedBackupEligibleCredential()
            => new("The stored credential is eligible for backup, but the provided credential was unexpectedly ineligible for backup.");

        public static PasskeyException ExpectedBackupIneligibleCredential()
            => new("The stored credential is ineligible for backup, but the provided credential was unexpectedly eligible for backup.");

        public static PasskeyException InvalidAssertionSignature()
            => new("The assertion signature was invalid.");

        public static PasskeyException SignCountLessThanOrEqualToStoredSignCount()
            => new("The authenticator's signature counter is unexpectedly less than or equal to the stored signature counter.");

        public static PasskeyException InvalidAttestationObject(Exception ex)
            => new($"An exception occurred while parsing the attestation object: {ex.Message}", ex);

        public static PasskeyException InvalidAttestationObjectFormat(Exception ex)
            => new("The attestation object had an invalid format.", ex);

        public static PasskeyException MissingAttestationStatementFormat()
            => new("The attestation object did not include an attestation statement format.");

        public static PasskeyException MissingAttestationStatement()
            => new("The attestation object did not include an attestation statement.");

        public static PasskeyException MissingAuthenticatorData()
            => new("The attestation object did not include authenticator data.");

        public static PasskeyException InvalidAuthenticatorDataLength(int length)
            => new($"The authenticator data had an invalid byte count of {length}.");

        public static PasskeyException InvalidAuthenticatorDataFormat(Exception? ex = null)
            => new($"The authenticator data had an invalid format.", ex);

        public static PasskeyException InvalidAttestedCredentialDataLength(int length)
            => new($"The attested credential data had an invalid byte count of {length}.");

        public static PasskeyException InvalidAttestedCredentialDataFormat(Exception? ex = null)
            => new($"The attested credential data had an invalid format.", ex);

        public static PasskeyException InvalidTokenBindingStatus(string tokenBindingStatus)
            => new($"Invalid token binding status '{tokenBindingStatus}'.");

        public static PasskeyException NullAttestationCredentialJson()
            => new("The attestation credential JSON was unexpectedly null.");

        public static PasskeyException InvalidAttestationCredentialJsonFormat(JsonException ex)
            => new($"The attestation credential JSON had an invalid format: {ex.Message}", ex);

        public static PasskeyException NullOriginalCreationOptionsJson()
            => new("The original passkey creation options were unexpectedly null.");

        public static PasskeyException InvalidOriginalCreationOptionsJsonFormat(JsonException ex)
            => new($"The original passkey creation options had an invalid format: {ex.Message}", ex);

        public static PasskeyException NullAssertionCredentialJson()
            => new("The assertion credential JSON was unexpectedly null.");

        public static PasskeyException InvalidAssertionCredentialJsonFormat(JsonException ex)
            => new($"The assertion credential JSON had an invalid format: {ex.Message}", ex);

        public static PasskeyException NullOriginalRequestOptionsJson()
            => new("The original passkey request options were unexpectedly null.");

        public static PasskeyException InvalidOriginalRequestOptionsJsonFormat(JsonException ex)
            => new($"The original passkey request options had an invalid format: {ex.Message}", ex);

        public static PasskeyException NullClientDataJson()
            => new("The client data JSON was unexpectedly null.");

        public static PasskeyException InvalidClientDataJsonFormat(JsonException ex)
            => new($"The client data JSON had an invalid format: {ex.Message}", ex);

        public static PasskeyException InvalidCredentialPublicKey(Exception ex)
            => new($"The credential public key was invalid.", ex);
    }
}
