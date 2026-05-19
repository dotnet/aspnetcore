// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

namespace Microsoft.Extensions.Internal;

public class SecurityHelperTests
{
    [Fact]
    public void AddingToAnonymousIdentityDoesNotKeepAnonymousIdentity()
    {
        var user = SecurityHelper.MergeUserPrincipal(new ClaimsPrincipal(), new GenericPrincipal(new GenericIdentity("Test1", "Alpha"), new string[0]));

        Assert.NotNull(user);
        Assert.Equal("Alpha", user.Identity.AuthenticationType);
        Assert.Equal("Test1", user.Identity.Name);
        Assert.IsAssignableFrom<ClaimsPrincipal>(user);
        Assert.IsAssignableFrom<ClaimsIdentity>(user.Identity);
        Assert.Single(user.Identities);
    }

    [Fact]
    public void AddingExistingIdentityChangesDefaultButPreservesPrior()
    {
        ClaimsPrincipal user = new GenericPrincipal(new GenericIdentity("Test1", "Alpha"), null);

        Assert.Equal("Alpha", user.Identity.AuthenticationType);
        Assert.Equal("Test1", user.Identity.Name);

        user = SecurityHelper.MergeUserPrincipal(user, new GenericPrincipal(new GenericIdentity("Test2", "Beta"), new string[0]));

        Assert.Equal("Beta", user.Identity.AuthenticationType);
        Assert.Equal("Test2", user.Identity.Name);

        user = SecurityHelper.MergeUserPrincipal(user, new GenericPrincipal(new GenericIdentity("Test3", "Gamma"), new string[0]));

        Assert.Equal("Gamma", user.Identity.AuthenticationType);
        Assert.Equal("Test3", user.Identity.Name);

        Assert.Equal(3, user.Identities.Count());
        Assert.Equal("Test3", user.Identities.Skip(0).First().Name);
        Assert.Equal("Test2", user.Identities.Skip(1).First().Name);
        Assert.Equal("Test1", user.Identities.Skip(2).First().Name);
    }

    [Fact]
    public void AddingPreservesNewIdentitiesAndDropsEmpty()
    {
        var existingPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var identityNoAuthTypeWithClaim = new ClaimsIdentity();
        identityNoAuthTypeWithClaim.AddClaim(new Claim("identityNoAuthTypeWithClaim", "yes"));
        existingPrincipal.AddIdentity(identityNoAuthTypeWithClaim);
        var identityEmptyWithAuthType = new ClaimsIdentity("empty");
        existingPrincipal.AddIdentity(identityEmptyWithAuthType);

        Assert.False(existingPrincipal.Identity.IsAuthenticated);

        var newPrincipal = new ClaimsPrincipal();
        var newEmptyIdentity = new ClaimsIdentity();
        var identityTwo = new ClaimsIdentity("yep");
        newPrincipal.AddIdentity(newEmptyIdentity);
        newPrincipal.AddIdentity(identityTwo);

        var user = SecurityHelper.MergeUserPrincipal(existingPrincipal, newPrincipal);

        // Preserve newPrincipal order
        Assert.False(user.Identity.IsAuthenticated);
        Assert.Null(user.Identity.Name);

        Assert.Equal(4, user.Identities.Count());
        Assert.Equal(newEmptyIdentity, user.Identities.Skip(0).First());
        Assert.Equal(identityTwo, user.Identities.Skip(1).First());
        Assert.Equal(identityNoAuthTypeWithClaim, user.Identities.Skip(2).First());
        Assert.Equal(identityEmptyWithAuthType, user.Identities.Skip(3).First());

        // This merge should drop newEmptyIdentity since its empty
        user = SecurityHelper.MergeUserPrincipal(user, new GenericPrincipal(new GenericIdentity("Test3", "Gamma"), new string[0]));

        Assert.Equal("Gamma", user.Identity.AuthenticationType);
        Assert.Equal("Test3", user.Identity.Name);

        Assert.Equal(4, user.Identities.Count());
        Assert.Equal("Test3", user.Identities.Skip(0).First().Name);
        Assert.Equal(identityTwo, user.Identities.Skip(1).First());
        Assert.Equal(identityNoAuthTypeWithClaim, user.Identities.Skip(2).First());
        Assert.Equal(identityEmptyWithAuthType, user.Identities.Skip(3).First());
    }
}
