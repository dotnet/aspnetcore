// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Identity;

[JsonSerializable(typeof(CollectedClientData))]
[JsonSerializable(typeof(PublicKeyCredentialCreationOptions))]
[JsonSerializable(typeof(PublicKeyCredentialRequestOptions))]
[JsonSerializable(typeof(PublicKeyCredential<AuthenticatorAssertionResponse>))]
[JsonSerializable(typeof(PublicKeyCredential<AuthenticatorAttestationResponse>))]
[JsonSerializable(typeof(PasskeyAttestationState))]
[JsonSerializable(typeof(PasskeyAssertionState))]
[JsonSerializable(typeof(AllAcceptedCredentialsSignalOptions))]
[JsonSerializable(typeof(CurrentUserDetailsSignalOptions))]
[JsonSerializable(typeof(PublicKeyCredentialId))]
[JsonSerializable(typeof(UnknownCredentialSignalOptions))]
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    RespectNullableAnnotations = true)]
internal partial class IdentityJsonSerializerContext : JsonSerializerContext;
