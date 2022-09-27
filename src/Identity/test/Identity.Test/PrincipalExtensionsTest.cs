// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Test;

public class ClaimsIdentityExtensionsTest
{
    public const string ExternalAuthenticationScheme = "TestExternalAuth";

    [Fact]
    public void IdentityExtensionsFindFirstValueNullIfUnknownTest()
    {
        var id = CreateTestExternalIdentity();
        Assert.Null(id.FindFirstValue("bogus"));
    }

    private static ClaimsPrincipal CreateTestExternalIdentity()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                    new Claim(ClaimTypes.NameIdentifier, "NameIdentifier", null, ExternalAuthenticationScheme),
                    new Claim(ClaimTypes.Name, "Name")
            },
            ExternalAuthenticationScheme));
    }
}
