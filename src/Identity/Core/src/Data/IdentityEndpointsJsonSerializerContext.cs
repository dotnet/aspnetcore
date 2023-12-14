// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Identity.Data;

[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(ForgotPasswordRequest))]
[JsonSerializable(typeof(ResendConfirmationEmailRequest))]
[JsonSerializable(typeof(InfoRequest))]
[JsonSerializable(typeof(InfoResponse))]
[JsonSerializable(typeof(TwoFactorRequest))]
[JsonSerializable(typeof(TwoFactorResponse))]
internal sealed partial class IdentityEndpointsJsonSerializerContext : JsonSerializerContext
{
}
