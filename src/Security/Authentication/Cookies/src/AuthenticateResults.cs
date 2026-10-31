// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Cookies;

internal static class AuthenticateResults
{
    internal static AuthenticateResult FailedUnprotectingTicket = AuthenticateResult.Fail("Unprotect ticket failed");
    internal static AuthenticateResult MissingSessionId = AuthenticateResult.Fail("SessionId missing");
    internal static AuthenticateResult MissingIdentityInSession = AuthenticateResult.Fail("Identity missing in session store");
    internal static AuthenticateResult ExpiredTicket = AuthenticateResult.Fail("Ticket expired");
    internal static AuthenticateResult NoPrincipal = AuthenticateResult.Fail("No principal.");
}
