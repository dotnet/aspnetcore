// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.DTO;

internal sealed class InfoRequest
{
    public string? NewEmail { get; init; }
    public string? NewPassword { get; init; }
    public string? OldPassword { get; init; }
}
