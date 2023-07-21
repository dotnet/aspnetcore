// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

internal static class AuthenticateResults
{
    internal static AuthenticateResult ValidatorNotFound = AuthenticateResult.Fail("No SecurityTokenValidator available for token.");
    internal static AuthenticateResult TokenHandlerUnableToValidate = AuthenticateResult.Fail("No TokenHandler was able to validate the token.");
}
