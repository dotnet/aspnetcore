// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
            foreach (var uri in redirectUris)
            {
                Assert.Contains(uri, registeredUris);
            }
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanGetScopesForApplication()
        {
            if (ShouldSkipDbTests())
            {
                return;
            }

            var manager = CreateManager();
            var application = CreateTestApplication();
            var scopes = GenerateScopes(nameof(CanGetScopesForApplication), 2);

            IdentityServiceResultAssert.IsSuccess(await manager.CreateAsync(application));
            foreach (var redirect in scopes)
            {
                await manager.AddScopeAsync(application, redirect);
            }

            var applicationScopes = await manager.FindScopesAsync(application);
            foreach (var scope in scopes)
            {
                Assert.Contains(scope, applicationScopes);
            }
        }

        private IEnumerable<string> GenerateRedirectUris(string prefix, int count) =>
            Enumerable.Range(0, count).Select(i => $"https://www.example.com/{prefix}/{count}");

        private IEnumerable<string> GenerateScopes(string prefix, int count) =>
            Enumerable.Range(0, count).Select(i => $"{prefix}_{count}");

        private class TestUser { }
        private class TestRole { }
    }
}
