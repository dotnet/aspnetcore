// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;
public record SigningKey(string Id, string Issuer, string Value, int Length)
{
}
