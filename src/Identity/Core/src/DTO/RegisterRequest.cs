// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.DTO;

internal sealed class RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
