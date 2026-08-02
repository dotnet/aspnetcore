// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Certificate;

internal static class AuthenticateResults
{
    internal static AuthenticateResult NoSelfSigned = AuthenticateResult.Fail("Options do not allow self signed certificates.");
    internal static AuthenticateResult NoChainedCertificates = AuthenticateResult.Fail("Options do not allow chained certificates.");
    internal static AuthenticateResult InvalidClientCertificate = AuthenticateResult.Fail("Client certificate failed validation.");
}
