// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class SecurityHelperTests
    {
        [Fact]
        public void AddingToAnonymousIdentityDoesNotKeepAnonymousIdentity()
        {
            HttpContext context = new DefaultHttpContext();
            context.User.ShouldNotBe(null);
            context.User.Identity.IsAuthenticated.ShouldBe(false);

            SecurityHelper.AddUserPrincipal(context, new GenericPrincipal(new GenericIdentity("Test1", "Alpha"), new string[0]));

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
            HttpContext context = new DefaultHttpContext();
            context.User = new GenericPrincipal(new GenericIdentity("Test1", "Alpha"), null);

            context.User.Identity.AuthenticationType.ShouldBe("Alpha");
            context.User.Identity.Name.ShouldBe("Test1");

            SecurityHelper.AddUserPrincipal(context, new GenericPrincipal(new GenericIdentity("Test2", "Beta"), new string[0]));

            context.User.Identity.AuthenticationType.ShouldBe("Beta");
            context.User.Identity.Name.ShouldBe("Test2");

            SecurityHelper.AddUserPrincipal(context, new GenericPrincipal(new GenericIdentity("Test3", "Gamma"), new string[0]));

            context.User.Identity.AuthenticationType.ShouldBe("Gamma");
            context.User.Identity.Name.ShouldBe("Test3");

            var principal = context.User;
            principal.Identities.Count().ShouldBe(3);
            principal.Identities.Skip(0).First().Name.ShouldBe("Test3");
            principal.Identities.Skip(1).First().Name.ShouldBe("Test2");
            principal.Identities.Skip(2).First().Name.ShouldBe("Test1");
        }
    }
}
