// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.DTO;

internal sealed class TwoFactorResponse
{
    public required string SharedKey { get; init; }
    public required int RecoveryCodesLeft { get; init; }
    public string[]? RecoveryCodes { get; init; }
    public required bool IsTwoFactorEnabled { get; init; }
    public required bool IsMachineRemembered { get; init; }
}
