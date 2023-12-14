// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed record JwtCreatorOptions(
    string Scheme,
    string Name,
    List<string> Audiences,
    string Issuer,
    DateTime NotBefore,
    DateTime ExpiresOn,
    List<string> Roles,
    List<string> Scopes,
    Dictionary<string, string> Claims);
