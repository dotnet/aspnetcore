// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authorization.Policy.Test
{
    public class PolicyEvaluatorTests
    {
        [Fact]
        public async Task AuthenticateFailsIfNoPrincipalReturned()
        {
            // Arrange
            var evaluator = new PolicyEvaluator(new HappyAuthorization());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection().AddSingleton<IAuthenticationService, SadAuthentication>();
            context.RequestServices = services.BuildServiceProvider();
            var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

            // Act
            var result = await evaluator.AuthenticateAsync(policy, context);

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task AuthenticateMergeSchemes()
        {
            // Arrange
            var evaluator = new PolicyEvaluator(new HappyAuthorization());
            var context = new DefaultHttpContext();
            var services = new ServiceCollection().AddSingleton<IAuthenticationService, EchoAuthentication>();
            context.RequestServices = services.BuildServiceProvider();
            var policy = new AuthorizationPolicyBuilder().AddAuthenticationSchemes("A","B","C").RequireAssertion(_ => true).Build();

            // Act
            var result = await evaluator.AuthenticateAsync(policy, context);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(3, result.Principal.Identities.Count());
        }


        [Fact]
        public async Task AuthorizeSucceedsEvenIfAuthenticationFails()
        {
            // Arrange
            var evaluator = new PolicyEvaluator(new HappyAuthorization());
            var context = new DefaultHttpContext();
            var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

            // Act
            var result = await evaluator.AuthorizeAsync(policy, AuthenticateResult.Fail("Nooo"), context);

            // Assert
            Assert.True(result.Succeeded);
            Assert.False(result.Challenged);
            Assert.False(result.Forbidden);
        }

        [Fact]
        public async Task AuthorizeChallengesIfAuthenticationFails()
        {
            // Arrange
            var evaluator = new PolicyEvaluator(new SadAuthorization());
            var context = new DefaultHttpContext();
            var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

            // Act
            var result = await evaluator.AuthorizeAsync(policy, AuthenticateResult.Fail("Nooo"), context);

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.Challenged);
            Assert.False(result.Forbidden);
        }

        [Fact]
        public async Task AuthorizeForbidsIfAuthenticationSuceeds()
        {
            // Arrange
            var evaluator = new PolicyEvaluator(new SadAuthorization());
            var context = new DefaultHttpContext();
            var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

            // Act
            var result = await evaluator.AuthorizeAsync(policy, AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "scheme")), context);

            // Assert
            Assert.False(result.Succeeded);
            Assert.False(result.Challenged);
            Assert.True(result.Forbidden);
        }

        public class HappyAuthorization : IAuthorizationService
        {
            public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
                => Task.FromResult(AuthorizationResult.Success());

            public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
                => Task.FromResult(AuthorizationResult.Success());
        }

        public class SadAuthorization : IAuthorizationService
        {
            public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
                => Task.FromResult(AuthorizationResult.Failed());

            public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
                => Task.FromResult(AuthorizationResult.Failed());
        }

        public class SadAuthentication : IAuthenticationService
        {
            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
            {
                return Task.FromResult(AuthenticateResult.Fail("Sad."));
            }

            public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                throw new System.NotImplementedException();
            }

            public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                throw new System.NotImplementedException();
            }

            public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
            {
                throw new System.NotImplementedException();
            }

            public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                throw new System.NotImplementedException();
            }
        }

        public class EchoAuthentication : IAuthenticationService
        {
            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
            {
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(scheme)), scheme)));
            }

            public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                throw new System.NotImplementedException();
            }

            public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                throw new System.NotImplementedException();
            }

            public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
            {
                throw new System.NotImplementedException();
            }

            public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                throw new System.NotImplementedException();
            }
        }

    }
}