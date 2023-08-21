// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.DTO;

internal sealed class ResetPasswordRequest
{
    public required string Email { get; init; }
    public required string ResetCode { get; init; }
    public required string NewPassword { get; init; }
}
