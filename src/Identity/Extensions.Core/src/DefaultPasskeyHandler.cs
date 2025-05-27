// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// The default passkey handler.
/// </summary>
public sealed partial class DefaultPasskeyHandler<TUser> : IPasskeyHandler<TUser>
    where TUser : class
{
    private readonly IPasskeyOriginValidator _originValidator;
    private readonly IPasskeyAttestationStatementVerifier? _attestationStatementVerifier;
    private readonly PasskeyOptions _passkeyOptions;

    /// <summary>
    /// Constructs a new <see cref="DefaultPasskeyHandler{TUser}"/> instance.
    /// </summary>
    /// <param name="options">The <see cref="IdentityOptions"/>.</param>
    /// <param name="originValidator">The <see cref="IPasskeyOriginValidator"/> for validating origins.</param>
    /// <param name="attestationStatementVerifier">An optional <see cref="IPasskeyAttestationStatementVerifier"/> for verifying attestation statements.</param>
    public DefaultPasskeyHandler(
        IOptions<IdentityOptions> options,
        IPasskeyOriginValidator originValidator,
        IPasskeyAttestationStatementVerifier? attestationStatementVerifier = null)
    {
        _originValidator = originValidator;
        _attestationStatementVerifier = attestationStatementVerifier;
        _passkeyOptions = options.Value.Passkey;
    }

    /// <inheritdoc/>
    public async Task<PasskeyAttestationResult> PerformAttestationAsync(string credentialJson, string originalOptionsJson, UserManager<TUser> userManager)
    {
        try
        {
            return await PerformAttestationCoreAsync(credentialJson, originalOptionsJson, userManager).ConfigureAwait(false);
        }
        catch (PasskeyException ex)
        {
            return PasskeyAttestationResult.Fail(ex);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                throw;
            }

            return PasskeyAttestationResult.Fail(new PasskeyException($"An unexpected error occurred during passkey attestation: {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<PasskeyAssertionResult<TUser>> PerformAssertionAsync(TUser? user, string credentialJson, string originalOptionsJson, UserManager<TUser> userManager)
    {
        try
        {
            return await PerformAssertionCoreAsync(user, credentialJson, originalOptionsJson, userManager).ConfigureAwait(false);
        }
        catch (PasskeyException ex)
        {
            return PasskeyAssertionResult.Fail<TUser>(ex);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                throw;
            }

            return PasskeyAssertionResult.Fail<TUser>(new PasskeyException($"An unexpected error occurred during passkey assertion: {ex.Message}", ex));
        }
    }

    private async Task<PasskeyAttestationResult> PerformAttestationCoreAsync(
        string credentialJson,
        string originalOptionsJson,
        UserManager<TUser> userManager)
    {
        // See: https://www.w3.org/TR/webauthn-3/#sctn-registering-a-new-credential
        // NOTE: Quotes from the spec may have been modified.
        // NOTE: Steps 1-3 are expected to have been performed prior to the execution of this method.

        var credential = JsonSerializer.Deserialize(credentialJson, IdentityJsonSerializerContext.Default.PublicKeyCredentialAuthenticatorAttestationResponse)
            ?? throw new InvalidOperationException("The attestation JSON was unexpectedly null.");
        var originalOptions = JsonSerializer.Deserialize(originalOptionsJson, IdentityJsonSerializerContext.Default.PublicKeyCredentialCreationOptions)
            ?? throw new InvalidOperationException("The original passkey creation options were unexpectedly null.");

        if (!string.Equals("public-key", credential.Type, StringComparison.Ordinal))
        {
            throw PasskeyException.InvalidCredentialType("public-key", credential.Type);
        }

        // 3. Let response be credential.response.
        var response = credential.Response;

        // 4. Let clientExtensionResults be the result of calling credential.getClientExtensionResults().
        // NOTE: Not currently supported.

        // 5. Let JSONtext be the result of running UTF-8 decode on the value of response.clientDataJSON.
        // 6. Let clientData, claimed as collected during the credential creation, be the result of running an implementation-specific JSON parser on JSONtext.
        var clientData = JsonSerializer.Deserialize(response.ClientDataJSON.AsSpan(), IdentityJsonSerializerContext.Default.CollectedClientData)
            ?? throw new InvalidOperationException("The client data JSON was unexpectedly null.");

        // 7. Verify that the value of clientData.type is webauthn.create.
        if (!string.Equals("webauthn.create", clientData.Type, StringComparison.Ordinal))
        {
            throw PasskeyException.InvalidClientDataType("webauthn.create", clientData.Type);
        }

        // 8. Verify that the value of clientData.challenge equals the base64url encoding of pkOptions.challenge.
        if (!clientData.Challenge.Equals(originalOptions.Challenge))
        {
            throw PasskeyException.InvalidChallenge();
        }

        // 9-11. Verify that the value of C.origin matches the Relying Party's origin.
        // NOTE: The level 3 draft permits having multiple origins and validating the "top origin" when a cross-origin request is made.
        //       For future-proofing, we pass a PasskeyOriginInfo to the origin validator so that we're able to add more properties to
        //       it later.
        var originInfo = new PasskeyOriginInfo(clientData.Origin, clientData.CrossOrigin);
        if (!_originValidator.IsValidOrigin(originInfo))
        {
            throw PasskeyException.InvalidOrigin(clientData.Origin);
        }

        // NOTE: The level 2 spec requires token binding validation, but the level 3 spec does not.
        //       We'll just validate that the token binding object doesn't have an unexpected format.
        if (clientData.TokenBinding is { } tokenBinding)
        {
            var status = tokenBinding.Status;
            if (!string.Equals("supported", status, StringComparison.Ordinal) &&
                !string.Equals("present", status, StringComparison.Ordinal) &&
                !string.Equals("not-supported", status, StringComparison.Ordinal))
            {
                throw PasskeyException.InvalidTokenBindingStatus(status);
            }
        }

        // 12. Let clientDataHash be the result of computing a hash over response.clientDataJSON using SHA-256.
        var clientDataHash = ComputeSHA256Hash(response.ClientDataJSON);

        // 13. Perform CBOR decoding on the attestationObject field of the AuthenticatorAttestationResponse structure and obtain the
        //     the authenticator data authenticatorData.
        var attestationObjectMemory = response.AttestationObject.AsMemory();
        if (!AttestationObject.TryParse(attestationObjectMemory, out var attestationObject))
        {
            throw PasskeyException.InvalidAttestationObject();
        }
        if (!AuthenticatorData.TryParse(attestationObject.AuthData, out var authenticatorData))
        {
            throw PasskeyException.InvalidAuthenticatorData();
        }

        // 14. Verify that the rpIdHash in authenticatorData is the SHA-256 hash of the RP ID expected by the Relying Party.
        var rpIdHash = ComputeSHA256Hash(Encoding.UTF8.GetBytes(originalOptions.Rp.Id ?? string.Empty));
        if (!authenticatorData.RpIdHash.Span.SequenceEqual(rpIdHash.AsSpan()))
        {
            throw PasskeyException.InvalidRelyingPartyIDHash();
        }

        // 15. If options.mediation is not set to conditional, verify that the UP bit of the flags in authData is set.
        // NOTE: We currently check for the UserPresent flag unconditionally. Consider making this optional via options.mediation
        //       after the level 3 draft becomes standard.
        if (!authenticatorData.IsUserPresent)
        {
            throw PasskeyException.UserNotPresent();
        }

        // 16. If user verification is required for this registration, verify that the User Verified bit of the flags in authData is set.
        if (string.Equals("required", originalOptions.AuthenticatorSelection?.UserVerification, StringComparison.Ordinal) && !authenticatorData.IsUserVerified)
        {
            throw PasskeyException.UserNotVerified();
        }

        // 17. If the BE bit of the flags in authData is not set, verify that the BS bit is not set.
        if (!authenticatorData.IsBackupEligible && authenticatorData.IsBackedUp)
        {
            throw PasskeyException.NotBackupEligibleYetBackedUp();
        }

        // 18. If the Relying Party uses the credential’s backup eligibility to inform its user experience flows and/or policies,
        //     evaluate the BE bit of the flags in authData.
        if (authenticatorData.IsBackupEligible && _passkeyOptions.BackupEligibleCredentialPolicy is PasskeyOptions.CredentialBackupPolicy.Disallowed)
        {
            throw PasskeyException.BackupEligibilityDisallowedYetBackupEligible();
        }
        if (!authenticatorData.IsBackupEligible && _passkeyOptions.BackupEligibleCredentialPolicy is PasskeyOptions.CredentialBackupPolicy.Required)
        {
            throw PasskeyException.BackupEligibilityRequiredYetNotBackupEligible();
        }

        // 19. If the Relying Party uses the credential’s backup state to inform its user experience flows and/or policies, evaluate the BS
        //     bit of the flags in authData.
        if (authenticatorData.IsBackedUp && _passkeyOptions.BackedUpCredentialPolicy is PasskeyOptions.CredentialBackupPolicy.Disallowed)
        {
            throw PasskeyException.BackupDisallowedYetBackedUp();
        }
        if (!authenticatorData.IsBackedUp && _passkeyOptions.BackedUpCredentialPolicy is PasskeyOptions.CredentialBackupPolicy.Required)
        {
            throw PasskeyException.BackupRequiredYetNotBackedUp();
        }

        // 20. Verify that the "alg" parameter in the credential public key in authData matches the alg attribute of one of the items in pkOptions.pubKeyCredParams.
        if (!authenticatorData.HasAttestedCredentialData)
        {
            throw PasskeyException.MissingAttestedCredentialData();
        }

        // The attested credential data should always be non-null if the 'HasAttestedCredentialData' flag is set.
        Debug.Assert(authenticatorData.AttestedCredentialData is not null);

        if (!originalOptions.PubKeyCredParams.Any(a => authenticatorData.AttestedCredentialData.CredentialPublicKey.Alg == a.Alg))
        {
            throw PasskeyException.UnsupportedCredentialPublicKeyAlgorithm();
        }

        // 21-24. Determine the attestation statement format by performing a USASCII case-sensitive match on fmt against the set of supported WebAuthn
        //        Attestation Statement Format Identifier values...
        if (_attestationStatementVerifier is not null)
        {
            // Handles all validation related to the attestation statement (21-24).
            var isAttestationStatementValid = await _attestationStatementVerifier.VerifyAsync(attestationObjectMemory, clientDataHash).ConfigureAwait(false);
            if (!isAttestationStatementValid)
            {
                throw PasskeyException.InvalidAttestationStatement();
            }
        }

        // 25. Verify that the credentialId is <= 1023 bytes.
        if (credential.Id is not { } credentialIdBufferSource)
        {
            throw PasskeyException.MissingCredentialId();
        }
        if (credentialIdBufferSource.Length is not > 0 and <= 1023)
        {
            throw PasskeyException.InvalidCredentialIdLength(credentialIdBufferSource.Length);
        }

        // 26. Verify that the credentialId is not yet registered for any user.
        var credentialId = credentialIdBufferSource.ToArray();
        var existingUser = await userManager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
        if (existingUser is not null)
        {
            throw PasskeyException.CredentialAlreadyRegistered();
        }

        // 27. Let credentialRecord be a new credential record with the following contents:
        var attestedCredentialData = authenticatorData.AttestedCredentialData;
        var credentialRecord = new UserPasskeyInfo(
            credentialId: credentialId,
            publicKey: attestedCredentialData.CredentialPublicKey.ToArray(),
            name: null,
            createdAt: DateTime.Now,
            signCount: authenticatorData.SignCount,
            transports: response.Transports,
            isUserVerified: authenticatorData.IsUserVerified,
            isBackupEligible: authenticatorData.IsBackupEligible,
            isBackedUp: authenticatorData.IsBackedUp,
            attestationObject: response.AttestationObject.ToArray(),
            clientDataJson: response.ClientDataJSON.ToArray());

        // 28. Process the client extension outputs in clientExtensionResults and the authenticator extension
        //     outputs in the extensions in authData as required by the Relying Party.
        // NOTE: Not currently supported.

        // 29. If all the above steps are successful, store credentialRecord in the user account that was denoted
        // and continue the registration ceremony as appropriate.
        return PasskeyAttestationResult.Success(credentialRecord);
    }

    private async Task<PasskeyAssertionResult<TUser>> PerformAssertionCoreAsync(
        TUser? user,
        string credentialJson,
        string originalOptionsJson,
        UserManager<TUser> userManager)
    {
        // See: https://www.w3.org/TR/webauthn-3/#sctn-registering-a-new-credential
        // NOTE: Quotes from the spec may have been modified.
        // NOTE: Steps 1-3 are expected to have been performed prior to the execution of this method.

        var credential = JsonSerializer.Deserialize(credentialJson, IdentityJsonSerializerContext.Default.PublicKeyCredentialAuthenticatorAssertionResponse)
            ?? throw new InvalidOperationException("The assertion JSON was unexpectedly null.");
        var originalOptions = JsonSerializer.Deserialize(originalOptionsJson, IdentityJsonSerializerContext.Default.PublicKeyCredentialRequestOptions)
            ?? throw new InvalidOperationException("The original passkey request options were unexpectedly null.");

        if (!string.Equals("public-key", credential.Type, StringComparison.Ordinal))
        {
            throw PasskeyException.InvalidCredentialType("public-key", credential.Type);
        }

        // 3. Let response be credential.response.
        var response = credential.Response;

        // 4. Let clientExtensionResults be the result of calling credential.getClientExtensionResults().
        // NOTE: Not currently supported.

        // 5. If originalOptions.allowCredentials is not empty, verify that credential.id identifies one of the public key
        //    credentials listed in pkOptions.allowCredentials.
        if (originalOptions.AllowCredentials is { Count: > 0 } allowCredentials &&
            !originalOptions.AllowCredentials.Any(c => c.Id.Equals(credential.Id)))
        {
            throw PasskeyException.CredentialNotAllowed();
        }

        var credentialId = credential.Id.ToArray();
        var userHandle = response.UserHandle?.ToString();
        UserPasskeyInfo? storedPasskey;

        // 6. Identify the user being authenticated and let credentialRecord be the credential record for the credential:
        if (user is not null)
        {
            // * If the user was identified before the authentication ceremony was initiated, e.g., via a username or cookie,
            //   verify that the identified user account contains a credential record whose id equals
            //   credential.rawId. Let credentialRecord be that credential record. If response.userHandle is
            //   present, verify that it equals the user handle of the user account.
            storedPasskey = await userManager.GetPasskeyAsync(user, credentialId).ConfigureAwait(false);
            if (storedPasskey is null)
            {
                throw PasskeyException.CredentialDoesNotBelongToUser();
            }
            if (userHandle is not null)
            {
                var userId = await userManager.GetUserIdAsync(user).ConfigureAwait(false);
                if (!string.Equals(userHandle, userId, StringComparison.Ordinal))
                {
                    throw PasskeyException.UserHandleMismatch(userId, userHandle);
                }
            }
        }
        else
        {
            // * If the user was not identified before the authentication ceremony was initiated,
            //   verify that response.userHandle is present. Verify that the user account identified by
            //   response.userHandle contains a credential record whose id equals credential.rawId. Let
            //   credentialRecord be that credential record.
            if (userHandle is null)
            {
                throw PasskeyException.MissingUserHandle();
            }

            user = await userManager.FindByIdAsync(userHandle).ConfigureAwait(false);
            if (user is null)
            {
                throw PasskeyException.CredentialDoesNotBelongToUser();
            }
            storedPasskey = await userManager.GetPasskeyAsync(user, credentialId).ConfigureAwait(false);
            if (storedPasskey is null)
            {
                throw PasskeyException.CredentialDoesNotBelongToUser();
            }
        }

        // 7. Let cData, authData and sig denote the value of response’s clientDataJSON, authenticatorData, and signature respectively.
        if (!AuthenticatorData.TryParse(response.AuthenticatorData.AsMemory(), out var authenticatorData))
        {
            throw PasskeyException.InvalidAuthenticatorData();
        }
        // 8. Let JSONtext be the result of running UTF-8 decode on the value of cData.
        // 9. Let C, the client data claimed as used for the signature, be the result of running an implementation-specific JSON parser on JSONtext.
        var clientData = JsonSerializer.Deserialize(response.ClientDataJSON.AsSpan(), IdentityJsonSerializerContext.Default.CollectedClientData)
            ?? throw new InvalidOperationException("The client data JSON was unexpectedly null.");

        // 10. Verify that the value of C.type is the string webauthn.get.
        if (!string.Equals("webauthn.get", clientData.Type, StringComparison.Ordinal))
        {
            throw PasskeyException.InvalidClientDataType("webauthn.get", clientData.Type);
        }

        // 11. Verify that the value of C.challenge equals the base64url encoding of originalOptions.challenge.
        if (!clientData.Challenge.Equals(originalOptions.Challenge))
        {
            throw PasskeyException.InvalidChallenge();
        }

        // 12-14. Verify that the value of C.origin is an origin expected by the Relying Party.
        // NOTE: The level 3 draft permits having multiple origins and validating the "top origin" when a cross-origin request is made.
        //       For future-proofing, we pass a PasskeyOriginInfo to the origin validator so that we're able to add more properties to
        //       it later.
        var originInfo = new PasskeyOriginInfo(clientData.Origin, clientData.CrossOrigin);
        if (!_originValidator.IsValidOrigin(originInfo))
        {
            throw PasskeyException.InvalidOrigin(clientData.Origin);
        }

        // NOTE: The level 2 spec requires token binding validation, but the level 3 spec does not.
        //       We'll just validate that the token binding object doesn't have an unexpected format.
        if (clientData.TokenBinding is { } tokenBinding)
        {
            var status = tokenBinding.Status;
            if (!string.Equals("supported", status, StringComparison.Ordinal) &&
                !string.Equals("present", status, StringComparison.Ordinal) &&
                !string.Equals("not-supported", status, StringComparison.Ordinal))
            {
                throw PasskeyException.InvalidTokenBindingStatus(status);
            }
        }

        // 15. Verify that the rpIdHash in authData is the SHA-256 hash of the RP ID expected by the Relying Party.
        var rpIdHash = ComputeSHA256Hash(Encoding.UTF8.GetBytes(originalOptions.RpId ?? string.Empty));
        if (!authenticatorData.RpIdHash.Span.SequenceEqual(rpIdHash.AsSpan()))
        {
            throw PasskeyException.InvalidRelyingPartyIDHash();
        }

        // 16. Verify that the UP bit of the flags in authData is set.
        if (!authenticatorData.IsUserPresent)
        {
            throw PasskeyException.UserNotPresent();
        }

        // 17. If user verification was determined to be required, verify that the UV bit of the flags in authData is set.
        //     Otherwise, ignore the value of the UV flag.
        if (string.Equals("required", originalOptions.UserVerification, StringComparison.Ordinal) && !authenticatorData.IsUserVerified)
        {
            throw PasskeyException.UserNotVerified();
        }

        // 18. If the BE bit of the flags in authData is not set, verify that the BS bit is not set.
        if (!authenticatorData.IsBackupEligible && authenticatorData.IsBackedUp)
        {
            throw PasskeyException.NotBackupEligibleYetBackedUp();
        }

        // 19. If the credential backup state is used as part of Relying Party business logic or policy, let currentBe and currentBs
        //     be the values of the BE and BS bits, respectively, of the flags in authData. Compare currentBe and currentBs with
        //     credentialRecord.backupEligible and credentialRecord.backupState:
        //     1. If credentialRecord.backupEligible is set, verify that currentBe is set.
        //     2. If credentialRecord.backupEligible is not set, verify that currentBe is not set.
        //     3. Apply Relying Party policy, if any.
        if (storedPasskey.IsBackupEligible && !authenticatorData.IsBackupEligible)
        {
            throw PasskeyException.ExpectedBackupEligibleCredential();
        }
        if (!storedPasskey.IsBackupEligible && authenticatorData.IsBackupEligible)
        {
            throw PasskeyException.ExpectedBackupIneligibleCredential();
        }
        if (authenticatorData.IsBackedUp && _passkeyOptions.BackedUpCredentialPolicy is PasskeyOptions.CredentialBackupPolicy.Disallowed)
        {
            throw PasskeyException.BackupDisallowedYetBackedUp();
        }
        if (!authenticatorData.IsBackedUp && _passkeyOptions.BackedUpCredentialPolicy is PasskeyOptions.CredentialBackupPolicy.Required)
        {
            throw PasskeyException.BackupRequiredYetNotBackedUp();
        }

        // 20. Let clientDataHash be the result of computing a hash over the cData using SHA-256.
        var clientDataHash = ComputeSHA256Hash(response.ClientDataJSON);

        // 21. Using credentialRecord.publicKey, verify that sig is a valid signature over the binary concatenation of authData and hash.
        byte[] data = [.. response.AuthenticatorData.AsSpan(), .. clientDataHash];
        var cpk = new CredentialPublicKey(storedPasskey.PublicKey);
        if (!cpk.Verify(data, response.Signature.AsSpan()))
        {
            throw PasskeyException.InvalidAssertionSignature();
        }

        // 22. If authData.signCount is nonzero or credentialRecord.signCount is nonzero, then run the following sub-step:
        if (authenticatorData.SignCount != 0 || storedPasskey.SignCount != 0)
        {
            // * If authData.signCount is greater than credentialRecord.signCount:
            //       The signature counter is valid.
            // * If authData.signCount is less than or equal to credentialRecord.signCount
            //       This is a signal, but not proof, that the authenticator may be cloned.
            //       NOTE: We simply fail the ceremony in this case.
            if (authenticatorData.SignCount <= storedPasskey.SignCount)
            {
                throw PasskeyException.SignCountLessThanStoredSignCount();
            }
        }

        // 23. Process the client extension outputs in clientExtensionResults and the authenticator extension outputs
        //     in the extensions in authData as required by the Relying Party.
        // NOTE: Not currently supported.

        // 24. Update credentialRecord with new state values
        //     1. Update credentialRecord.signCount to the value of authData.signCount.
        storedPasskey.SignCount = authenticatorData.SignCount;

        //     2. Update credentialRecord.backupState to the value of currentBs.
        storedPasskey.IsBackedUp = authenticatorData.IsBackedUp;

        //     3. If credentialRecord.uvInitialized is false, update it to the value of the UV bit in the flags in authData.
        //     This change SHOULD require authorization by an additional authentication factor equivalent to WebAuthn user verification;
        //     if not authorized, skip this step.
        // NOTE: Not currently supported.

        // 25. If all the above steps are successful, continue the authentication ceremony as appropriate.
        return PasskeyAssertionResult.Success(storedPasskey, user);
    }

    private static byte[] ComputeSHA256Hash(byte[] data)
    {
#if NETCOREAPP
        return SHA256.HashData(data);
#else
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
#endif
    }

    private static byte[] ComputeSHA256Hash(BufferSource data)
    {
#if NETCOREAPP
        return SHA256.HashData(data.AsSpan());
#else
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data.ToArray());
#endif
    }
}
