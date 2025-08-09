// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

public class IdentityPasskeyOptionsTest
{
    [Fact]
    public void VerifyDefaultOptions()
    {
        var options = new IdentityPasskeyOptions();

        Assert.Equal(TimeSpan.FromMinutes(5), options.AuthenticatorTimeout);
        Assert.Equal(32, options.ChallengeSize);
        Assert.Equal("preferred", options.ResidentKeyRequirement);
        Assert.Equal("required", options.UserVerificationRequirement);
        Assert.Null(options.ServerDomain);
        Assert.Null(options.AttestationConveyancePreference);
        Assert.Null(options.AuthenticatorAttachment);
        Assert.Null(options.IsAllowedAlgorithm);
        Assert.Null(options.ValidateOrigin);
        Assert.Null(options.VerifyAttestationStatement);
    }
}
