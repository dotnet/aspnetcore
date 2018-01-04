// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service.Specification.Tests
{
    /// <summary>
    /// Common functionality tests that verifies all user manager functionality regardless of store implementation.
    /// </summary>
    public abstract class IdentityServiceSpecificationTestBase<TUser, TApplication> : IdentityServiceSpecificationTestBase<TUser, TApplication, string, string>
        where TUser : class
        where TApplication : class
    { }

    /// <summary>
    /// Base class for tests that exercise basic identity functionality that all stores should support.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <typeparam name="TApplication">The type of the application.</typeparam>
    /// <typeparam name="TUserKey">The primary key type for the user.</typeparam>
    /// <typeparam name="TApplicationKey">The primary key type for the application.</typeparam>
    public abstract class IdentityServiceSpecificationTestBase<TUser, TApplication, TUserKey, TApplicationKey>
        where TUser : class
        where TApplication : class
        where TUserKey : IEquatable<TUserKey>
        where TApplicationKey : IEquatable<TApplicationKey>
    {
        private const string NullValue = "(null)";

        /// <summary>
        /// If true, test that require a database will be skipped.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldSkipDbTests() => false;

        /// <summary>
        /// Configure the service collection used for tests.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="context"></param>
        protected virtual void SetupIdentityServiceServices(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            new IdentityBuilder(typeof(TestUser), typeof(TestRole), services)
                .AddApplications<TUser, TApplication>(options => { });

            services.AddSingleton<IApplicationValidator<TApplication>, ClaimValidator>();

            AddApplicationStore(services, context);
            services.AddLogging();
            services.AddSingleton<ILogger<ApplicationManager<TApplication>>>(new TestLogger<ApplicationManager<TApplication>>());
        }

        /// <summary>
        /// Creates the application manager used for tests.
        /// </summary>
        /// <param name="context">The context that will be passed into the store, typically a db context.</param>
        /// <param name="services">The service collection to use, optional.</param>
        /// <param name="configureServices">Delegate used to configure the services, optional.</param>
        /// <returns>The application manager to use for tests.</returns>
        protected virtual ApplicationManager<TApplication> CreateManager(object context = null, IServiceCollection services = null, Action<IServiceCollection> configureServices = null)
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }
            if (context == null)
            {
                context = CreateTestContext();
            }
            SetupIdentityServiceServices(services, context);
            configureServices?.Invoke(services);
            return services.BuildServiceProvider().GetService<ApplicationManager<TApplication>>();
        }

        /// <summary>
        /// Creates the context object for a test, typically a DbContext.
        /// </summary>
        /// <returns>The context object for a test, typically a DbContext.</returns>
        protected abstract object CreateTestContext();

        /// <summary>
        /// Adds an IApplicationStore to services for the test.
        /// </summary>
        /// <param name="services">The service collection to add to.</param>
        /// <param name="context">The context for the store to use, optional.</param>
        protected abstract void AddApplicationStore(IServiceCollection services, object context = null);

        /// <summary>
        /// Creates an application instance for testing.
        /// </summary>
        /// <returns></returns>
        protected abstract TApplication CreateTestApplication();

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task.</returns>
        [Fact]
        public async Task CanDeleteApplication()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }
            var manager = CreateManager();
            var application = CreateTestApplication();
            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            var applicationId = await manager.GetApplicationIdAsync(application);
            IdentityServiceResultAssert.IsSuccess(await manager.DeleteAsync(application));
            Assert.Null(await manager.FindByIdAsync(applicationId));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanFindById()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }
            var manager = CreateManager();
            var application = CreateTestApplication();
            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.NotNull(await manager.FindByIdAsync(await manager.GetApplicationIdAsync(application)));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanFindByName()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }
            var manager = CreateManager();
            var application = CreateTestApplication();
            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.NotNull(await manager.FindByNameAsync(await manager.GetApplicationNameAsync(application)));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task SetApplicationName()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }
            var manager = CreateManager();
            var application = CreateTestApplication();
            var name = await manager.GetApplicationNameAsync(application);
            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            var newName = Guid.NewGuid().ToString();
            Assert.Null(await manager.FindByNameAsync(newName));
            IdentityServiceResultAssert.IsSuccess(await manager.SetApplicationNameAsync(application, newName));
            Assert.Null(await manager.FindByNameAsync(name));
            Assert.NotNull(await manager.FindByNameAsync(newName));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task SetApplicationName_ValidatesNewName()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }
            var manager = CreateManager();
            var application = CreateTestApplication();
            var name = await manager.GetApplicationNameAsync(application);
            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            var newName = "";
            Assert.Null(await manager.FindByNameAsync(newName));
            IdentityServiceResultAssert.IsFailure(await manager.SetApplicationNameAsync(application, newName));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanFindByClientId()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }
            var manager = CreateManager();
            var application = CreateTestApplication();
            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.NotNull(await manager.FindByClientIdAsync(await manager.GetApplicationClientIdAsync(application)));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanGetRedirectUrisForApplication()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirectUris = GenerateRedirectUris(nameof(CanGetRedirectUrisForApplication), 2);

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            foreach (var redirect in redirectUris)
            {
                await manager.RegisterRedirectUriAsync(application, redirect);
            }

            var registeredUris = await manager.FindRegisteredUrisAsync(application);
            foreach (var uri in registeredUris)
            {
                Assert.Contains(uri, redirectUris);
            }
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RegisterRedirectUrisForApplicationValidatesUris()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = "";

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredUrisAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.RegisterRedirectUriAsync(application, redirect));
            Assert.Empty(await manager.FindRegisteredUrisAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateRedirectUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("login", 2).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredUrisAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.RegisterRedirectUriAsync(application, redirect[0]));
            Assert.Equal(redirect[0], (await manager.FindRegisteredUrisAsync(application)).First());
            IdentityServiceResultAssert.IsSuccess(await manager.UpdateRedirectUriAsync(application, redirect[0], redirect[1]));
            Assert.Equal(redirect[1], (await manager.FindRegisteredUrisAsync(application)).First());
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateRedirectUriValidatesRedirectUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("login", 1).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredUrisAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.RegisterRedirectUriAsync(application, redirect[0]));
            Assert.Equal(redirect[0], (await manager.FindRegisteredUrisAsync(application)).First());
            IdentityServiceResultAssert.IsFailure(await manager.UpdateRedirectUriAsync(application, redirect[0], ""));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateRedirectUriFailsIfItDoesNotFindTheUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("login", 2).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredUrisAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.UpdateRedirectUriAsync(application, redirect[0], redirect[1]));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUnregisterRedirectUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("login", 1).Single();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredUrisAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.RegisterRedirectUriAsync(application, redirect));
            Assert.Equal(redirect, (await manager.FindRegisteredUrisAsync(application)).Single());
            IdentityServiceResultAssert.IsSuccess(await manager.UnregisterRedirectUriAsync(application, redirect));
            Assert.Empty(await manager.FindRegisteredUrisAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UnregisterRedirectUriFailsIfItDoesNotFindTheUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("login", 1).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredUrisAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.UnregisterRedirectUriAsync(application, redirect[0]));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanGetLogoutUrisForApplication()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var logoutUris = GenerateRedirectUris(nameof(CanGetLogoutUrisForApplication), 2);

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            foreach (var logout in logoutUris)
            {
                await manager.RegisterLogoutUriAsync(application, logout);
            }

            var registeredLogoutUris = await manager.FindRegisteredLogoutUrisAsync(application);
            foreach (var uri in registeredLogoutUris)
            {
                Assert.Contains(uri, logoutUris);
            }
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RegisterLogoutUrisForApplicationValidatesUris()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = "";

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredLogoutUrisAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.RegisterLogoutUriAsync(application, redirect));
            Assert.Empty(await manager.FindRegisteredLogoutUrisAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateLogoutUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("logout", 2).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredLogoutUrisAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.RegisterLogoutUriAsync(application, redirect[0]));
            Assert.Equal(redirect[0], (await manager.FindRegisteredLogoutUrisAsync(application)).First());
            IdentityServiceResultAssert.IsSuccess(await manager.UpdateLogoutUriAsync(application, redirect[0], redirect[1]));
            Assert.Equal(redirect[1], (await manager.FindRegisteredLogoutUrisAsync(application)).First());
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateLogoutUriValidatesRedirectUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("logout", 1).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredLogoutUrisAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.RegisterLogoutUriAsync(application, redirect[0]));
            Assert.Equal(redirect[0], (await manager.FindRegisteredLogoutUrisAsync(application)).First());
            IdentityServiceResultAssert.IsFailure(await manager.UpdateLogoutUriAsync(application, redirect[0], ""));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateLogoutUriFailsIfItDoesNotFindTheUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("logout", 2).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredLogoutUrisAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.UpdateLogoutUriAsync(application, redirect[0], redirect[1]));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUnregisterLogoutUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("logout", 1).Single();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredLogoutUrisAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.RegisterLogoutUriAsync(application, redirect));
            Assert.Equal(redirect, (await manager.FindRegisteredLogoutUrisAsync(application)).Single());
            IdentityServiceResultAssert.IsSuccess(await manager.UnregisterLogoutUriAsync(application, redirect));
            Assert.Empty(await manager.FindRegisteredLogoutUrisAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UnregisterLogoutUriFailsIfItDoesNotFindTheUri()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var redirect = GenerateRedirectUris("logout", 1).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindRegisteredLogoutUrisAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.UnregisterLogoutUriAsync(application, redirect[0]));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanGetScopes()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scopes = GenerateScopes(nameof(CanGetScopes), 2);

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            foreach (var scope in scopes)
            {
                await manager.AddScopeAsync(application, scope);
            }

            var applicationScopes = await manager.FindScopesAsync(application);
            foreach (var scope in applicationScopes)
            {
                Assert.Contains(scope, scopes);
            }
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanAddScopes()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scope = "offline_access";

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindScopesAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.AddScopeAsync(application, scope));
            Assert.NotEmpty(await manager.FindScopesAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AddScopesValidatesScopes()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scope = "";

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindScopesAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.AddScopeAsync(application, scope));
            Assert.Empty(await manager.FindScopesAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateScopes()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scopes = GenerateScopes("UpdateScopes", 2).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindScopesAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.AddScopeAsync(application, scopes[0]));
            Assert.Equal(scopes[0], (await manager.FindScopesAsync(application)).First());
            IdentityServiceResultAssert.IsSuccess(await manager.UpdateScopeAsync(application, scopes[0], scopes[1]));
            Assert.Equal(scopes[1], (await manager.FindScopesAsync(application)).First());
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateScopeValidatesScope()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scopes = GenerateScopes("ValidateScope", 1).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindScopesAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.AddScopeAsync(application, scopes[0]));
            Assert.Equal(scopes[0], (await manager.FindScopesAsync(application)).First());
            IdentityServiceResultAssert.IsFailure(await manager.UpdateScopeAsync(application, scopes[0], ""));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateScopeFailsIfItDoesNotFindTheScope()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scope = GenerateScopes("UpdateScopeNoScope", 2).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindScopesAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.UpdateScopeAsync(application, scope[0], scope[1]));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanRemoveScope()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scope = GenerateScopes(nameof(CanRemoveScope), 1).Single();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindScopesAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.AddScopeAsync(application, scope));
            Assert.Equal(scope, (await manager.FindScopesAsync(application)).Single());
            IdentityServiceResultAssert.IsSuccess(await manager.RemoveScopeAsync(application, scope));
            Assert.Empty(await manager.FindScopesAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RemoveScopeFailsIfItDoesNotFindTheScope()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scope = GenerateScopes("RemoveScopeValidates", 1).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.FindScopesAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.RemoveScopeAsync(application, scope[0]));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanGetClaimsForApplication()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var claims = GenerateClaims(nameof(CanGetClaimsForApplication), 2);

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            foreach (var claim in claims)
            {
                await manager.AddClaimAsync(application, claim);
            }

            var applicationClaims = await manager.GetClaimsAsync(application);
            foreach (var claim in applicationClaims)
            {
                Assert.Contains(claim, claims, ClaimComparer.Instance);
            }
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanAddClaims()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var claim = new Claim("type", "value");

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.GetClaimsAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.AddClaimAsync(application, claim));
            Assert.NotEmpty(await manager.GetClaimsAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AddClaimsValidatesClaims()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scope = new Claim("fail", "fail");

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.GetClaimsAsync(application));
            IdentityServiceResultAssert.IsFailure(await manager.AddClaimAsync(application, scope));
            Assert.Empty(await manager.GetClaimsAsync(application));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateClaims()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var claims = GenerateClaims("UpdateClaims", 2).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.GetClaimsAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.AddClaimAsync(application, claims[0]));
            Assert.Equal(claims[0], (await manager.GetClaimsAsync(application)).First(), ClaimComparer.Instance);
            IdentityServiceResultAssert.IsSuccess(await manager.ReplaceClaimAsync(application, claims[0], claims[1]));
            Assert.Equal(claims[1], (await manager.GetClaimsAsync(application)).First(), ClaimComparer.Instance);
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ReplaceClaimValidatesClaim()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var claims = GenerateClaims("ValidateClaim", 1).ToArray();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.GetClaimsAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.AddClaimAsync(application, claims[0]));
            Assert.Equal(claims[0], (await manager.GetClaimsAsync(application)).First(), ClaimComparer.Instance);
            IdentityServiceResultAssert.IsFailure(await manager.ReplaceClaimAsync(application, claims[0], new Claim("fail", "fail")));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanRemoveClaim()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var claim = GenerateClaims(nameof(CanRemoveClaim), 1).Single();

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            Assert.Empty(await manager.GetClaimsAsync(application));
            IdentityServiceResultAssert.IsSuccess(await manager.AddClaimAsync(application, claim));
            Assert.Equal(claim, (await manager.GetClaimsAsync(application)).Single(),ClaimComparer.Instance);
            IdentityServiceResultAssert.IsSuccess(await manager.RemoveClaimAsync(application, claim));
            Assert.Empty(await manager.GetClaimsAsync(application));
        }

        private IEnumerable<string> GenerateRedirectUris(string prefix, int count) =>
            Enumerable.Range(0, count).Select(i => $"https://www.example.com/{prefix}/{i}");

        private IEnumerable<string> GenerateScopes(string prefix, int count) =>
            Enumerable.Range(0, count).Select(i => $"{prefix}_{i}");

        private IEnumerable<Claim> GenerateClaims(string prefix, int count) =>
            Enumerable.Range(0, count).Select(i => new Claim($"{prefix}_type_{i}", $"{prefix}_value_{i}"));

        private class ClaimComparer : IEqualityComparer<Claim>
        {
            public static readonly ClaimComparer Instance = new ClaimComparer();

            public bool Equals(Claim x, Claim y) => x?.Type == y?.Type && x?.Value == y?.Value;

            public int GetHashCode(Claim obj)
            {
                throw new NotImplementedException();
            }
        }

        private class ClaimValidator : IApplicationValidator<TApplication>
        {
            public Task<IdentityServiceResult> ValidateAsync(ApplicationManager<TApplication> manager, TApplication application)
            {
                return Task.FromResult(IdentityServiceResult.Success);
            }

            public Task<IdentityServiceResult> ValidateClaimAsync(ApplicationManager<TApplication> manager, TApplication application, Claim claim)
            {
                return Task.FromResult(claim.Type.Equals("fail") ? IdentityServiceResult.Failed(new IdentityServiceError()) : IdentityServiceResult.Success);
            }

            public Task<IdentityServiceResult> ValidateLogoutUriAsync(ApplicationManager<TApplication> manager, TApplication application, string logoutUri)
            {
                return Task.FromResult(IdentityServiceResult.Success);
            }

            public Task<IdentityServiceResult> ValidateRedirectUriAsync(ApplicationManager<TApplication> manager, TApplication application, string redirectUri)
            {
                return Task.FromResult(IdentityServiceResult.Success);
            }

            public Task<IdentityServiceResult> ValidateScopeAsync(ApplicationManager<TApplication> manager, TApplication application, string scope)
            {
                return Task.FromResult(IdentityServiceResult.Success);
            }
        }

        private class TestUser { }
        private class TestRole { }
    }
}
