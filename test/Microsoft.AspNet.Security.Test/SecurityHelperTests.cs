// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Security.Infrastructure;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Security
{
    public class SecurityHelperTests
    {
        [Fact]
        public void AddingToAnonymousIdentityDoesNotKeepAnonymousIdentity()
        {
            HttpContext context = new DefaultHttpContext();
            context.User.ShouldNotBe(null);
            context.User.Identity.IsAuthenticated.ShouldBe(false);

            SecurityHelper.AddUserIdentity(context, new GenericIdentity("Test1", "Alpha"));

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

            SecurityHelper.AddUserIdentity(context, new GenericIdentity("Test2", "Beta"));

            context.User.Identity.AuthenticationType.ShouldBe("Beta");
            context.User.Identity.Name.ShouldBe("Test2");

            SecurityHelper.AddUserIdentity(context, new GenericIdentity("Test3", "Gamma"));

            context.User.Identity.AuthenticationType.ShouldBe("Gamma");
            context.User.Identity.Name.ShouldBe("Test3");

            var principal = context.User;
            principal.Identities.Count().ShouldBe(3);
            principal.Identities.Skip(0).First().Name.ShouldBe("Test3");
            principal.Identities.Skip(1).First().Name.ShouldBe("Test2");
            principal.Identities.Skip(2).First().Name.ShouldBe("Test1");
        }

        [Fact]
        public void NoChallengesMeansLookupsAreDeterminedOnlyByActiveOrPassiveMode()
        {
            HttpContext context = new DefaultHttpContext();

            bool activeNoChallenge = SecurityHelper.LookupChallenge(new string[0], "Alpha", AuthenticationMode.Active);
            bool passiveNoChallenge = SecurityHelper.LookupChallenge(new string[0], "Alpha", AuthenticationMode.Passive);

            context.Response.StatusCode = 401;

            bool activeEmptyChallenge = SecurityHelper.LookupChallenge(new string[0], "Alpha", AuthenticationMode.Active);
            bool passiveEmptyChallenge = SecurityHelper.LookupChallenge(new string[0], "Alpha", AuthenticationMode.Passive);

            Assert.True(activeNoChallenge);
            Assert.False(passiveNoChallenge);
            Assert.True(activeEmptyChallenge);
            Assert.False(passiveEmptyChallenge);
        }

        [Fact]
        public void WithChallengesMeansLookupsAreDeterminedOnlyByMatchingAuthenticationType()
        {
            HttpContext context = new DefaultHttpContext();
            
            IEnumerable<string> challengeTypes = new[] { "Beta", "Gamma" };

            bool activeNoMatch = SecurityHelper.LookupChallenge(challengeTypes, "Alpha", AuthenticationMode.Active);
            bool passiveNoMatch = SecurityHelper.LookupChallenge(challengeTypes, "Alpha", AuthenticationMode.Passive);

            challengeTypes = new[] { "Beta", "Alpha" };

            bool activeWithMatch = SecurityHelper.LookupChallenge(challengeTypes, "Alpha", AuthenticationMode.Active);
            bool passiveWithMatch = SecurityHelper.LookupChallenge(challengeTypes, "Alpha", AuthenticationMode.Passive);

            Assert.False(activeNoMatch);
            Assert.False(passiveNoMatch);
            Assert.True(activeWithMatch);
            Assert.True(passiveWithMatch);
        }
    }
}
