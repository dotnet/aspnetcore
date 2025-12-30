// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// The default passkey handler.
/// </summary>
public sealed class PasskeyHandler<TUser> : IPasskeyHandler<TUser>
    where TUser : class
{
    private readonly UserManager<TUser> _userManager;
    private readonly IdentityPasskeyOptions _options;

    /// <summary>
    /// Constructs a new <see cref="PasskeyHandler{TUser}"/> instance.
    /// </summary>
    /// <param name="userManager">The <see cref="UserManager{TUser}"/>.</param>
    /// <param name="options">The <see cref="IdentityOptions"/>.</param>
    public PasskeyHandler(UserManager<TUser> userManager, IOptions<IdentityPasskeyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(options);

        _userManager = userManager;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<PasskeyCreationOptionsResult> MakeCreationOptionsAsync(PasskeyUserEntity userEntity, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(userEntity);
        ArgumentNullException.ThrowIfNull(httpContext);

        var excludeCredentials = await GetExcludeCredentialsAsync().ConfigureAwait(false);
        var serverDomain = GetServerDomain(httpContext);
        var challenge = RandomNumberGenerator.GetBytes(_options.ChallengeSize);
        var pubKeyCredParams = _options.IsAllowedAlgorithm is { } isAllowedAlgorithm
            ? [.. CredentialPublicKey.AllSupportedParameters.Where(p => isAllowedAlgorithm((int)p.Alg))]
            : CredentialPublicKey.AllSupportedParameters;
        var options = new PublicKeyCredentialCreationOptions
        {
            Rp = new()
            {
                Name = serverDomain,
                Id = serverDomain,
            },
            User = new()
            {
                Id = BufferSource.FromString(userEntity.Id),
                Name = userEntity.Name,
                DisplayName = userEntity.DisplayName,
            },
            Challenge = BufferSource.FromBytes(challenge),
            Timeout = (uint)_options.AuthenticatorTimeout.TotalMilliseconds,
            ExcludeCredentials = excludeCredentials,
            PubKeyCredParams = pubKeyCredParams,
            AuthenticatorSelection = new()
            {
                AuthenticatorAttachment = _options.AuthenticatorAttachment,
                ResidentKey = _options.ResidentKeyRequirement,
                UserVerification = _options.UserVerificationRequirement,
            },
            Attestation = _options.AttestationConveyancePreference,
        };
        var attestationState = new PasskeyAttestationState
        {
            Challenge = challenge,
            UserEntity = userEntity,
        };
        var optionsJson = JsonSerializer.Serialize(options, IdentityJsonSerializerContext.Default.PublicKeyCredentialCreationOptions);
        var attestationStateJson = JsonSerializer.Serialize(attestationState, IdentityJsonSerializerContext.Default.PasskeyAttestationState);
        var creationOptions = new PasskeyCreationOptionsResult
        {
            CreationOptionsJson = optionsJson,
            AttestationState = attestationStateJson,
        };

        return creationOptions;

        async Task<PublicKeyCredentialDescriptor[]> GetExcludeCredentialsAsync()
        {
            var existingUser = await _userManager.FindByIdAsync(userEntity.Id).ConfigureAwait(false);
            if (existingUser is null)
            {
                return [];
            }

            var passkeys = await _userManager.GetPasskeysAsync(existingUser).ConfigureAwait(false);
            var excludeCredentials = passkeys
                .Select(p => new PublicKeyCredentialDescriptor
                {
                    Type = "public-key",
                    Id = BufferSource.FromBytes(p.CredentialId),
                    Transports = p.Transports ?? [],
                });
            return [.. excludeCredentials];
        }
    }

    /// <inheritdoc />
    public async Task<PasskeyRequestOptionsResult> MakeRequestOptionsAsync(TUser? user, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var allowCredentials = await GetAllowCredentialsAsync().ConfigureAwait(false);
        var serverDomain = GetServerDomain(httpContext);
        var challenge = RandomNumberGenerator.GetBytes(_options.ChallengeSize);
        var options = new PublicKeyCredentialRequestOptions
        {
            Challenge = BufferSource.FromBytes(challenge),
            RpId = serverDomain,
            Timeout = (uint)_options.AuthenticatorTimeout.TotalMilliseconds,
            AllowCredentials = allowCredentials,
            UserVerification = _options.UserVerificationRequirement,
        };
        var userId = user is not null ? await _userManager.GetUserIdAsync(user).ConfigureAwait(false) : null;
        var assertionState = new PasskeyAssertionState
        {
            Challenge = challenge,
            UserId = userId,
        };
        var optionsJson = JsonSerializer.Serialize(options, IdentityJsonSerializerContext.Default.PublicKeyCredentialRequestOptions);
        var assertionStateJson = JsonSerializer.Serialize(assertionState, IdentityJsonSerializerContext.Default.PasskeyAssertionState);
        var requestOptions = new PasskeyRequestOptionsResult
        {
            RequestOptionsJson = optionsJson,
            AssertionState = assertionStateJson,
        };

        return requestOptions;

        async Task<PublicKeyCredentialDescriptor[]> GetAllowCredentialsAsync()
        {
            if (user is null)
            {
                return [];
            }

            var passkeys = await _userManager.GetPasskeysAsync(user).ConfigureAwait(false);
            var allowCredentials = passkeys
                .Select(p => new PublicKeyCredentialDescriptor
                {
                    Type = "public-key",
                    Id = BufferSource.FromBytes(p.CredentialId),
                    Transports = p.Transports ?? [],
                });
            return [.. allowCredentials];
        }
    }

    /// <inheritdoc/>
    public async Task<PasskeyAttestationResult> PerformAttestationAsync(PasskeyAttestationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await PerformAttestationCoreAsync(context).ConfigureAwait(false);
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
    public async Task<PasskeyAssertionResult<TUser>> PerformAssertionAsync(PasskeyAssertionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await PerformAssertionCoreAsync(context).ConfigureAwait(false);
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

    /// <summary>
    /// Performs passkey attestation using the provided credential JSON and original options JSON.
    /// </summary>
    /// <param name="context">The context containing necessary information for passkey attestation.</param>
    /// <returns>A task object representing the asynchronous operation containing the <see cref="PasskeyAttestationResult"/>.</returns>
    private async Task<PasskeyAttestationResult> PerformAttestationCoreAsync(PasskeyAttestationContext context)
    {
        // See: https://www.w3.org/TR/webauthn-3/#sctn-registering-a-new-credential
        // NOTE: Quotes from the spec may have been modified.
        // NOTE: Steps 1-3 are expected to have been performed prior to the execution of this method.

        PublicKeyCredential<AuthenticatorAttestationResponse> credential;
        PasskeyAttestationState attestationState;

        try
        {
            credential = JsonSerializer.Deserialize(context.CredentialJson, IdentityJsonSerializerContext.Default.PublicKeyCredentialAuthenticatorAttestationResponse)
                ?? throw PasskeyException.NullAttestationCredentialJson();
        }
        catch (JsonException ex)
        {
            throw PasskeyException.InvalidAttestationCredentialJsonFormat(ex);
        }

        try
        {
            if (context.AttestationState is not { } attestationStateJson)
            {
                throw PasskeyException.NullAttestationStateJson();
            }

            attestationState = JsonSerializer.Deserialize(attestationStateJson, IdentityJsonSerializerContext.Default.PasskeyAttestationState)
                ?? throw PasskeyException.NullAttestationStateJson();
        }
        catch (JsonException ex)
        {
            throw PasskeyException.InvalidAttestationStateJsonFormat(ex);
        }

        VerifyCredentialType(credential);

        // 3. Let response be credential.response.
        var response = credential.Response;

        // 4. Let clientExtensionResults be the result of calling credential.getClientExtensionResults().
        // NOTE: Not currently supported.

        // 5. Let JSONtext be the result of running UTF-8 decode on the value of response.clientDataJSON.
        // 6. Let clientData, claimed as collected during the credential creation, be the result of running an implementation-specific JSON parser on JSONtext.
        // 7. Verify that the value of clientData.type is webauthn.create.
        // 8. Verify that the value of clientData.challenge equals the base64url encoding of pkOptions.challenge.
        // 9-11. Verify that the value of C.origin matches the Relying Party's origin.
        await VerifyClientDataAsync(
            utf8Json: response.ClientDataJSON.AsMemory(),
            originalChallenge: attestationState.Challenge,
            expectedType: "webauthn.create",
            context.HttpContext)
            .ConfigureAwait(false);

        // 12. Let clientDataHash be the result of computing a hash over response.clientDataJSON using SHA-256.
        var clientDataHash = SHA256.HashData(response.ClientDataJSON.AsSpan());

        // 13. Perform CBOR decoding on the attestationObject field of the AuthenticatorAttestationResponse structure and obtain the
        //     the authenticator data authenticatorData.
        var attestationObjectMemory = response.AttestationObject.AsMemory();
        var attestationObject = AttestationObject.Parse(attestationObjectMemory);
        var authenticatorData = AuthenticatorData.Parse(attestationObject.AuthenticatorData);

        // 14. Verify that the rpIdHash in authenticatorData is the SHA-256 hash of the RP ID expected by the Relying Party.
        // 15. If options.mediation is not set to conditional, verify that the UP bit of the flags in authData is set.
        // 16. If user verification is required for this registration, verify that the User Verified bit of the flags in authData is set.
        // 17. If the BE bit of the flags in authData is not set, verify that the BS bit is not set.
        // 18. If the Relying Party uses the credential's backup eligibility to inform its user experience flows and/or policies,
        //     evaluate the BE bit of the flags in authData.
        // 19. If the Relying Party uses the credential's backup state to inform its user experience flows and/or policies, evaluate the BS
        //     bit of the flags in authData.
        //     NOTE: It's up to application code to evaluate BE and BS flags on the returned passkey and determine
        //           whether any action should be taken based on them.
        VerifyAuthenticatorData(authenticatorData, context.HttpContext);

        if (!authenticatorData.HasAttestedCredentialData)
        {
            throw PasskeyException.MissingAttestedCredentialData();
        }

        // 20. Verify that the "alg" parameter in the credential public key in authData matches the alg attribute of one of the items in pkOptions.pubKeyCredParams.
        var attestedCredentialData = authenticatorData.AttestedCredentialData;
        var algorithm = attestedCredentialData.CredentialPublicKey.Alg;
        if (!CredentialPublicKey.IsSupportedAlgorithm(algorithm))
        {
            // The algorithm is not implemented.
            throw PasskeyException.UnsupportedCredentialPublicKeyAlgorithm();
        }
        if (_options.IsAllowedAlgorithm is { } isAllowedAlgorithm && !isAllowedAlgorithm((int)algorithm))
        {
            // The algorithm is disallowed by the application.
            throw PasskeyException.UnsupportedCredentialPublicKeyAlgorithm();
        }

        // 21-24. Determine the attestation statement format by performing a USASCII case-sensitive match on fmt against the set of supported WebAuthn
        //        Attestation Statement Format Identifier values...
        //        Handles all validation related to the attestation statement (21-24).
        if (_options.VerifyAttestationStatement is { } verifyAttestationStatement)
        {
            var isAttestationStatementValid = await verifyAttestationStatement(new()
            {
                HttpContext = context.HttpContext,
                AttestationObject = attestationObjectMemory,
                ClientDataHash = clientDataHash,
            }).ConfigureAwait(false);

            if (!isAttestationStatementValid)
            {
                throw PasskeyException.InvalidAttestationStatement();
            }
        }

        // 25. Verify that the credentialId is <= 1023 bytes.
        // NOTE: Handled while parsing the attested credential data.
        if (!credential.Id.AsSpan().SequenceEqual(attestedCredentialData.CredentialId.Span))
        {
            throw PasskeyException.CredentialIdMismatch();
        }

        var credentialId = attestedCredentialData.CredentialId.ToArray();

        // 26. Verify that the credentialId is not yet registered for any user.
        var existingUser = await _userManager.FindByPasskeyIdAsync(credentialId).ConfigureAwait(false);
        if (existingUser is not null)
        {
            throw PasskeyException.CredentialAlreadyRegistered();
        }

        // 27. Let credentialRecord be a new credential record with the following contents:
        var credentialRecord = new UserPasskeyInfo(
            credentialId,
            publicKey: attestedCredentialData.CredentialPublicKey.ToArray(),
            createdAt: DateTime.UtcNow,
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
        return PasskeyAttestationResult.Success(credentialRecord, attestationState.UserEntity);
    }

    /// <summary>
    /// Performs passkey assertion using the provided credential JSON, original options JSON, and optional user.
    /// </summary>
    /// <param name="context">The context containing necessary information for passkey assertion.</param>
    /// <returns>A task object representing the asynchronous operation containing the <see cref="PasskeyAssertionResult{TUser}"/>.</returns>
    private async Task<PasskeyAssertionResult<TUser>> PerformAssertionCoreAsync(PasskeyAssertionContext context)
    {
        // See https://www.w3.org/TR/webauthn-3/#sctn-verifying-assertion
        // NOTE: Quotes from the spec may have been modified.
        // NOTE: Steps 1-3 are expected to have been performed prior to the execution of this method.

        PublicKeyCredential<AuthenticatorAssertionResponse> credential;
        PasskeyAssertionState assertionState;

        try
        {
            credential = JsonSerializer.Deserialize(context.CredentialJson, IdentityJsonSerializerContext.Default.PublicKeyCredentialAuthenticatorAssertionResponse)
                ?? throw PasskeyException.NullAssertionCredentialJson();
        }
        catch (JsonException ex)
        {
            throw PasskeyException.InvalidAssertionCredentialJsonFormat(ex);
        }

        try
        {
            if (context.AssertionState is not { } assertionStateJson)
            {
                throw PasskeyException.NullAssertionStateJson();
            }

            assertionState = JsonSerializer.Deserialize(assertionStateJson, IdentityJsonSerializerContext.Default.PasskeyAssertionState)
                ?? throw PasskeyException.NullAssertionStateJson();
        }
        catch (JsonException ex)
        {
            throw PasskeyException.InvalidAssertionStateJsonFormat(ex);
        }

        TUser? user = null;
        var originalUserId = assertionState.UserId;
        if (originalUserId is not null)
        {
            user = await _userManager.FindByIdAsync(originalUserId).ConfigureAwait(false)
                ?? throw PasskeyException.CredentialDoesNotBelongToUser();
        }

        VerifyCredentialType(credential);

        // 3. Let response be credential.response.
        var response = credential.Response;

        // 4. Let clientExtensionResults be the result of calling credential.getClientExtensionResults().
        // NOTE: Not currently supported.

        // 5. If originalOptions.allowCredentials is not empty, verify that credential.id identifies one of the public key
        //    credentials listed in pkOptions.allowCredentials.
        //    NOTE: Since we always include the user's full list of credentials in the options,
        //          we can simply check that the credential ID is present on the user.
        //          If we change this behavior, we may need to explicitly handle this step.

        var credentialId = credential.Id.ToArray();
        var userHandle = response.UserHandle?.ToString();
        UserPasskeyInfo? storedPasskey;

        // 6. Identify the user being authenticated and let credentialRecord be the credential record for the credential:
        if (user is not null)
        {
            // The user should only be non-null if the user ID was provided in the properties.
            Debug.Assert(originalUserId is not null);

            // * If the user was identified before the authentication ceremony was initiated, e.g., via a username or cookie,
            //   verify that the identified user account contains a credential record whose id equals
            //   credential.rawId. Let credentialRecord be that credential record. If response.userHandle is
            //   present, verify that it equals the user handle of the user account.
            storedPasskey = await _userManager.GetPasskeyAsync(user, credentialId).ConfigureAwait(false);
            if (storedPasskey is null)
            {
                throw PasskeyException.CredentialDoesNotBelongToUser();
            }
            if (userHandle is not null && !string.Equals(originalUserId, userHandle, StringComparison.Ordinal))
            {
                throw PasskeyException.UserHandleMismatch(originalUserId, userHandle);
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

            user = await _userManager.FindByIdAsync(userHandle).ConfigureAwait(false);
            if (user is null)
            {
                throw PasskeyException.CredentialDoesNotBelongToUser();
            }
            storedPasskey = await _userManager.GetPasskeyAsync(user, credentialId).ConfigureAwait(false);
            if (storedPasskey is null)
            {
                throw PasskeyException.CredentialDoesNotBelongToUser();
            }
        }

        // 7. Let cData, authData and sig denote the value of response's clientDataJSON, authenticatorData, and signature respectively.
        var authenticatorData = AuthenticatorData.Parse(response.AuthenticatorData.AsMemory());

        // 8. Let JSONtext be the result of running UTF-8 decode on the value of cData.
        // 9. Let C, the client data claimed as used for the signature, be the result of running an implementation-specific JSON parser on JSONtext.
        // 10. Verify that the value of C.type is the string webauthn.get.
        // 11. Verify that the value of C.challenge equals the base64url encoding of originalOptions.challenge.
        // 12-14. Verify that the value of C.origin is an origin expected by the Relying Party.
        await VerifyClientDataAsync(
            utf8Json: response.ClientDataJSON.AsMemory(),
            originalChallenge: assertionState.Challenge,
            expectedType: "webauthn.get",
            context.HttpContext)
            .ConfigureAwait(false);

        // 15. Verify that the rpIdHash in authData is the SHA-256 hash of the RP ID expected by the Relying Party.
        // 16. Verify that the UP bit of the flags in authData is set.
        // 17. If user verification was determined to be required, verify that the UV bit of the flags in authData is set.
        //     Otherwise, ignore the value of the UV flag.
        // 18. If the BE bit of the flags in authData is not set, verify that the BS bit is not set.
        VerifyAuthenticatorData(authenticatorData, context.HttpContext);

        // 19. If the credential backup state is used as part of Relying Party business logic or policy, let currentBe and currentBs
        //     be the values of the BE and BS bits, respectively, of the flags in authData. Compare currentBe and currentBs with
        //     credentialRecord.backupEligible and credentialRecord.backupState:
        //     1. If credentialRecord.backupEligible is set, verify that currentBe is set.
        //     2. If credentialRecord.backupEligible is not set, verify that currentBe is not set.
        //     3. Apply Relying Party policy, if any.
        //        NOTE: Additional RP policies should be handled by application code.
        if (storedPasskey.IsBackupEligible && !authenticatorData.IsBackupEligible)
        {
            throw PasskeyException.ExpectedBackupEligibleCredential();
        }
        if (!storedPasskey.IsBackupEligible && authenticatorData.IsBackupEligible)
        {
            throw PasskeyException.ExpectedBackupIneligibleCredential();
        }

        // 20. Let clientDataHash be the result of computing a hash over the cData using SHA-256.
        var clientDataHash = SHA256.HashData(response.ClientDataJSON.AsSpan());

        // 21. Using credentialRecord.publicKey, verify that sig is a valid signature over the binary concatenation of authData and hash.
        byte[] data = [.. response.AuthenticatorData.AsSpan(), .. clientDataHash];
        var cpk = CredentialPublicKey.Decode(storedPasskey.PublicKey);
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
                throw PasskeyException.SignCountLessThanOrEqualToStoredSignCount();
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

    private static void VerifyCredentialType<TResponse>(PublicKeyCredential<TResponse> credential)
        where TResponse : AuthenticatorResponse
    {
        const string ExpectedType = "public-key";
        if (!string.Equals(ExpectedType, credential.Type, StringComparison.Ordinal))
        {
            throw PasskeyException.InvalidCredentialType(ExpectedType, credential.Type);
        }
    }

    private async Task VerifyClientDataAsync(
        ReadOnlyMemory<byte> utf8Json,
        ReadOnlyMemory<byte> originalChallenge,
        string expectedType,
        HttpContext httpContext)
    {
        // Let JSONtext be the result of running UTF-8 decode on the value of cData.
        // Let C, the client data claimed as used for the signature, be the result of running an implementation-specific JSON parser on JSONtext.
        CollectedClientData clientData;
        try
        {
            clientData = JsonSerializer.Deserialize(utf8Json.Span, IdentityJsonSerializerContext.Default.CollectedClientData)
                ?? throw PasskeyException.NullClientDataJson();
        }
        catch (JsonException ex)
        {
            throw PasskeyException.InvalidClientDataJsonFormat(ex);
        }

        // Verify that the value of C.type is either the string webauthn.create or webauthn.get.
        // NOTE: The expected value depends on whether we're performing attestation or assertion.
        if (!string.Equals(expectedType, clientData.Type, StringComparison.Ordinal))
        {
            throw PasskeyException.InvalidClientDataType(expectedType, clientData.Type);
        }

        // Verify that the value of C.challenge equals the base64url encoding of originalOptions.challenge.
        if (!CryptographicOperations.FixedTimeEquals(clientData.Challenge.AsSpan(), originalChallenge.Span))
        {
            throw PasskeyException.InvalidChallenge();
        }

        // Verify that the value of C.origin is an origin expected by the Relying Party.
        var isOriginValid = await ValidateOriginAsync(clientData, httpContext).ConfigureAwait(false);
        if (!isOriginValid)
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
    }

    private void VerifyAuthenticatorData(
        AuthenticatorData authenticatorData,
        HttpContext httpContext)
    {
        // Verify that the rpIdHash in authenticatorData is the SHA-256 hash of the RP ID expected by the Relying Party.
        var originalRpId = GetServerDomain(httpContext);
        var originalRpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(originalRpId ?? string.Empty));
        if (!CryptographicOperations.FixedTimeEquals(authenticatorData.RpIdHash.Span, originalRpIdHash.AsSpan()))
        {
            throw PasskeyException.InvalidRelyingPartyIDHash();
        }

        // If options.mediation is not set to conditional, verify that the UP bit of the flags in authData is set.
        // NOTE: We currently check for the UserPresent flag unconditionally. Consider making this optional via options.mediation
        //       after the level 3 draft becomes standard.
        if (!authenticatorData.IsUserPresent)
        {
            throw PasskeyException.UserNotPresent();
        }

        // If user verification is required for this registration, verify that the User Verified bit of the flags in authData is set.
        var originalUserVerificationRequirement = _options.UserVerificationRequirement;
        if (string.Equals("required", originalUserVerificationRequirement, StringComparison.Ordinal) && !authenticatorData.IsUserVerified)
        {
            throw PasskeyException.UserNotVerified();
        }

        // If the BE bit of the flags in authData is not set, verify that the BS bit is not set.
        if (!authenticatorData.IsBackupEligible && authenticatorData.IsBackedUp)
        {
            throw PasskeyException.NotBackupEligibleYetBackedUp();
        }
    }

    private ValueTask<bool> ValidateOriginAsync(CollectedClientData clientData, HttpContext httpContext)
    {
        if (_options.ValidateOrigin is { } validateOrigin)
        {
            // The user has overridden the default origin validation,
            // so we'll use that instead of the default behavior.
            return validateOrigin(new PasskeyOriginValidationContext
            {
                HttpContext = httpContext,
                Origin = clientData.Origin,
                CrossOrigin = clientData.CrossOrigin == true,
                TopOrigin = clientData.TopOrigin,
            });
        }

        if (string.IsNullOrEmpty(clientData.Origin) ||
            clientData.CrossOrigin == true ||
            !Uri.TryCreate(clientData.Origin, UriKind.Absolute, out var originUri))
        {
            return ValueTask.FromResult(false);
        }

        // Uri.Equals correctly handles string comparands.
        return ValueTask.FromResult(httpContext.Request.Headers.Origin is [var origin] && originUri.Equals(origin));
    }

    private string GetServerDomain(HttpContext httpContext)
        => _options.ServerDomain ?? httpContext.Request.Host.Host;
}
