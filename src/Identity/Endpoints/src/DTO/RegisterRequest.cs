// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Endpoints.DTO;

// TODO: Register DTOs with JsonSerializerOptions.TypeInfoResolverChain (was previously the soon-to-be-obsolete AddContext)
internal sealed class RegisterRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    // TODO: public string? Email { get; set; }
}
