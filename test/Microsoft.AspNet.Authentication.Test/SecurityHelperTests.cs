// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Http.Internal;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class SecurityHelperTests
    {
        [Fact]
        public void AddingToAnonymousIdentityDoesNotKeepAnonymousIdentity()
        {
            var context = new DefaultHttpContext();
            context.User.ShouldNotBe(null);
            context.User.Identity.IsAuthenticated.ShouldBe(false);

            context.User = SecurityHelper.MergeUserPrincipal(context.User, new GenericPrincipal(new GenericIdentity("Test1", "Alpha"), new string[0]));

            context.User.ShouldNotBe(null);
            context.User.Identity.AuthenticationType.ShouldBe("Alpha");
            context.User.Identity.Name.ShouldBe("Test1");

            context.User.ShouldBeTypeOf<ClaimsPrincipal>();
            context.User.Identity.ShouldBeTypeOf<ClaimsIdentity>();

            ((ClaimsPrincipal)context.User).Identities.Count().ShouldBe(1);
        }

        [Fact]
        public void AddingExistingIdentityChangesDefaultButPreservesPrior()
        {
            var context = new DefaultHttpContext();
            context.User = new GenericPrincipal(new GenericIdentity("Test1", "Alpha"), null);

            context.User.Identity.AuthenticationType.ShouldBe("Alpha");
            context.User.Identity.Name.ShouldBe("Test1");

            context.User = SecurityHelper.MergeUserPrincipal(context.User, new GenericPrincipal(new GenericIdentity("Test2", "Beta"), new string[0]));

            context.User.Identity.AuthenticationType.ShouldBe("Beta");
            context.User.Identity.Name.ShouldBe("Test2");

            context.User = SecurityHelper.MergeUserPrincipal(context.User, new GenericPrincipal(new GenericIdentity("Test3", "Gamma"), new string[0]));

            context.User.Identity.AuthenticationType.ShouldBe("Gamma");
            context.User.Identity.Name.ShouldBe("Test3");

            var principal = context.User;
            principal.Identities.Count().ShouldBe(3);
            principal.Identities.Skip(0).First().Name.ShouldBe("Test3");
            principal.Identities.Skip(1).First().Name.ShouldBe("Test2");
            principal.Identities.Skip(2).First().Name.ShouldBe("Test1");
        }

        [Fact]
        public void AddingPreservesNewIdentitiesAndDropsEmpty()
        {
            var context = new DefaultHttpContext();
            var existingPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            var identityNoAuthTypeWithClaim = new ClaimsIdentity();
            identityNoAuthTypeWithClaim.AddClaim(new Claim("identityNoAuthTypeWithClaim", "yes"));
            existingPrincipal.AddIdentity(identityNoAuthTypeWithClaim);
            var identityEmptyWithAuthType = new ClaimsIdentity("empty");
            existingPrincipal.AddIdentity(identityEmptyWithAuthType);
            context.User = existingPrincipal;

            context.User.Identity.IsAuthenticated.ShouldBe(false);

            var newPrincipal = new ClaimsPrincipal();
            var newEmptyIdentity = new ClaimsIdentity();
            var identityTwo = new ClaimsIdentity("yep");
            newPrincipal.AddIdentity(newEmptyIdentity);
            newPrincipal.AddIdentity(identityTwo);

            context.User = SecurityHelper.MergeUserPrincipal(context.User, newPrincipal);

            // Preserve newPrincipal order
            context.User.Identity.IsAuthenticated.ShouldBe(false);
            context.User.Identity.Name.ShouldBe(null);

            var principal = context.User;
            principal.Identities.Count().ShouldBe(4);
            principal.Identities.Skip(0).First().ShouldBe(newEmptyIdentity);
            principal.Identities.Skip(1).First().ShouldBe(identityTwo);
            principal.Identities.Skip(2).First().ShouldBe(identityNoAuthTypeWithClaim);
            principal.Identities.Skip(3).First().ShouldBe(identityEmptyWithAuthType);

            // This merge should drop newEmptyIdentity since its empty
            context.User = SecurityHelper.MergeUserPrincipal(context.User, new GenericPrincipal(new GenericIdentity("Test3", "Gamma"), new string[0]));

            context.User.Identity.AuthenticationType.ShouldBe("Gamma");
            context.User.Identity.Name.ShouldBe("Test3");

            principal = context.User;
            principal.Identities.Count().ShouldBe(4);
            principal.Identities.Skip(0).First().Name.ShouldBe("Test3");
            principal.Identities.Skip(1).First().ShouldBe(identityTwo);
            principal.Identities.Skip(2).First().ShouldBe(identityNoAuthTypeWithClaim);
            principal.Identities.Skip(3).First().ShouldBe(identityEmptyWithAuthType);
        }
    }
}
