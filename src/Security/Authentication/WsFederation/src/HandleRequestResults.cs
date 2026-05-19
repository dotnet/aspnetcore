// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.WsFederation;

internal static class HandleRequestResults
{
    internal static HandleRequestResult NoMessage = HandleRequestResult.Fail("No message.");
    internal static HandleRequestResult UnsolicitedLoginsNotAllowed  = HandleRequestResult.Fail("Unsolicited logins are not allowed.");
}
