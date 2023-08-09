// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.DTO;

internal sealed class TwoFactorRequest
{
    public bool? Enable { get; init; }
    public string? TwoFactorCode { get; init; }

    public bool ResetSharedKey { get; init; }
    public bool ResetRecoveryCodes { get; init; }
    public bool ForgetMachine { get; init; }
}
