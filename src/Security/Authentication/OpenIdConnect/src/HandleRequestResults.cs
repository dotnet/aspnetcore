// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

internal static class HandleRequestResults
{
    internal static HandleRequestResult UnexpectedParams = HandleRequestResult.Fail("An OpenID Connect response cannot contain an identity token or an access token when using response_mode=query");
    internal static HandleRequestResult NoMessage = HandleRequestResult.Fail("No message.");
}
