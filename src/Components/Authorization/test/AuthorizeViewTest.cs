// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.Authorization
{
    public class AuthorizeViewTest
    {
        // Nothing should exceed the timeout in a successful run of the the tests, this is just here to catch
        // failures.
        private static readonly TimeSpan Timeout = Debugger.IsAttached ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10);

        [Fact]
        public void RendersNothingIfNotAuthorized()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView(
                childContent:
                    context => builder => builder.AddContent(0, "This should not be rendered"));

            // Act
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();

            // Assert
            var diff = renderer.Batches.Single().GetComponentDiffs<AuthorizeView>().Single();
            Assert.Empty(diff.Edits);

            // Assert: The IAuthorizationService was given expected criteria
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Null(call.user.Identity);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements,
                    req => Assert.IsType<DenyAnonymousAuthorizationRequirement>(req));
            });
        }

        [Fact]
        public void RendersNotAuthorizedIfNotAuthorized()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView(
                notAuthorized:
                    context => builder => builder.AddContent(0, $"You are not authorized, even though we know you are {context.User.Identity.Name}"));
            rootComponent.AuthenticationState = CreateAuthenticationState("Nellie");

            // Act
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();

            // Assert
            var diff = renderer.Batches.Single().GetComponentDiffs<AuthorizeView>().Single();
            Assert.Collection(diff.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    renderer.Batches.Single().ReferenceFrames[edit.ReferenceFrameIndex],
                    "You are not authorized, even though we know you are Nellie");
            });

            // Assert: The IAuthorizationService was given expected criteria
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Nellie", call.user.Identity.Name);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements,
                    req => Assert.IsType<DenyAnonymousAuthorizationRequirement>(req));
            });
        }

        [Fact]
        public void RendersNothingIfAuthorizedButNoChildContentOrAuthorizedProvided()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            authorizationService.NextResult = AuthorizationResult.Success();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView();
            rootComponent.AuthenticationState = CreateAuthenticationState("Nellie");

            // Act
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();

            // Assert
            var diff = renderer.Batches.Single().GetComponentDiffs<AuthorizeView>().Single();
            Assert.Empty(diff.Edits);

            // Assert: The IAuthorizationService was given expected criteria
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Nellie", call.user.Identity.Name);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements,
                    req => Assert.IsType<DenyAnonymousAuthorizationRequirement>(req));
            });
        }

        [Fact]
        public void RendersChildContentIfAuthorized()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            authorizationService.NextResult = AuthorizationResult.Success();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView(
                childContent: context => builder =>
                    builder.AddContent(0, $"You are authenticated as {context.User.Identity.Name}"));
            rootComponent.AuthenticationState = CreateAuthenticationState("Nellie");

            // Act
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();

            // Assert
            var diff = renderer.Batches.Single().GetComponentDiffs<AuthorizeView>().Single();
            Assert.Collection(diff.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    renderer.Batches.Single().ReferenceFrames[edit.ReferenceFrameIndex],
                    "You are authenticated as Nellie");
            });

            // Assert: The IAuthorizationService was given expected criteria
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Nellie", call.user.Identity.Name);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements,
                    req => Assert.IsType<DenyAnonymousAuthorizationRequirement>(req));
            });
        }

        [Fact]
        public void RendersAuthorizedIfAuthorized()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            authorizationService.NextResult = AuthorizationResult.Success();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView(
                authorized: context => builder =>
                    builder.AddContent(0, $"You are authenticated as {context.User.Identity.Name}"));
            rootComponent.AuthenticationState = CreateAuthenticationState("Nellie");

            // Act
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();

            // Assert
            var diff = renderer.Batches.Single().GetComponentDiffs<AuthorizeView>().Single();
            Assert.Collection(diff.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    renderer.Batches.Single().ReferenceFrames[edit.ReferenceFrameIndex],
                    "You are authenticated as Nellie");
            });

            // Assert: The IAuthorizationService was given expected criteria
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Nellie", call.user.Identity.Name);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements,
                    req => Assert.IsType<DenyAnonymousAuthorizationRequirement>(req));
            });
        }

        [Fact]
        public void RespondsToChangeInAuthorizationState()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            authorizationService.NextResult = AuthorizationResult.Success();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView(
                childContent: context => builder =>
                    builder.AddContent(0, $"You are authenticated as {context.User.Identity.Name}"));
            rootComponent.AuthenticationState = CreateAuthenticationState("Nellie");

            // Render in initial state. From other tests, we know this renders
            // a single batch with the correct output.
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();
            var authorizeViewComponentId = renderer.Batches.Single()
                .GetComponentFrames<AuthorizeView>().Single().ComponentId;
            authorizationService.AuthorizeCalls.Clear();

            // Act
            rootComponent.AuthenticationState = CreateAuthenticationState("Ronaldo");
            rootComponent.TriggerRender();

            // Assert: It's only one new diff. We skip the intermediate "await" render state
            // because the task was completed synchronously.
            Assert.Equal(2, renderer.Batches.Count);
            var batch = renderer.Batches.Last();
            var diff = batch.DiffsByComponentId[authorizeViewComponentId].Single();
            Assert.Collection(diff.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                AssertFrame.Text(
                    batch.ReferenceFrames[edit.ReferenceFrameIndex],
                    "You are authenticated as Ronaldo");
            });

            // Assert: The IAuthorizationService was given expected criteria
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Ronaldo", call.user.Identity.Name);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements,
                    req => Assert.IsType<DenyAnonymousAuthorizationRequirement>(req));
            });
        }

        [Fact]
        public void ThrowsIfBothChildContentAndAuthorizedProvided()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView(
                authorized: context => builder => { },
                childContent: context => builder => { });

            // Act/Assert
            renderer.AssignRootComponentId(rootComponent);
            var ex = Assert.Throws<InvalidOperationException>(() =>
                rootComponent.TriggerRender());
            Assert.Equal("Do not specify both 'Authorized' and 'ChildContent'.", ex.Message);
        }

        [Fact]
        public void RendersNothingUntilAuthorizationCompleted()
        {
            // Arrange
            var @event = new ManualResetEventSlim();
            var authorizationService = new TestAuthorizationService();
            var renderer = CreateTestRenderer(authorizationService);
            renderer.OnUpdateDisplayComplete = () => { @event.Set(); };
            var rootComponent = WrapInAuthorizeView(
                notAuthorized:
                    context => builder => builder.AddContent(0, "You are not authorized"));
            var authTcs = new TaskCompletionSource<AuthenticationState>();
            rootComponent.AuthenticationState = authTcs.Task;

            // Act/Assert 1: Auth pending
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();
            var batch1 = renderer.Batches.Single();
            var authorizeViewComponentId = batch1.GetComponentFrames<AuthorizeView>().Single().ComponentId;
            var diff1 = batch1.DiffsByComponentId[authorizeViewComponentId].Single();
            Assert.Empty(diff1.Edits);

            // Act/Assert 2: Auth process completes asynchronously
            @event.Reset();
            authTcs.SetResult(new AuthenticationState(new ClaimsPrincipal()));

            // We need to wait here because the continuations of SetResult will be scheduled to run asynchronously.
            @event.Wait(Timeout);

            Assert.Equal(2, renderer.Batches.Count);
            var batch2 = renderer.Batches[1];
            var diff2 = batch2.DiffsByComponentId[authorizeViewComponentId].Single();
            Assert.Collection(diff2.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    batch2.ReferenceFrames[edit.ReferenceFrameIndex],
                    "You are not authorized");
            });
        }

        [Fact]
        public void RendersAuthorizingUntilAuthorizationCompleted()
        {
            // Arrange
            var @event = new ManualResetEventSlim();
            var authorizationService = new TestAuthorizationService();
            authorizationService.NextResult = AuthorizationResult.Success();
            var renderer = CreateTestRenderer(authorizationService);
            renderer.OnUpdateDisplayComplete = () => { @event.Set(); };
            var rootComponent = WrapInAuthorizeView(
                authorizing: builder => builder.AddContent(0, "Auth pending..."),
                authorized: context => builder => builder.AddContent(0, $"Hello, {context.User.Identity.Name}!"));
            var authTcs = new TaskCompletionSource<AuthenticationState>();
            rootComponent.AuthenticationState = authTcs.Task;

            // Act/Assert 1: Auth pending
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();
            var batch1 = renderer.Batches.Single();
            var authorizeViewComponentId = batch1.GetComponentFrames<AuthorizeView>().Single().ComponentId;
            var diff1 = batch1.DiffsByComponentId[authorizeViewComponentId].Single();
            Assert.Collection(diff1.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    batch1.ReferenceFrames[edit.ReferenceFrameIndex],
                    "Auth pending...");
            });

            // Act/Assert 2: Auth process completes asynchronously
            @event.Reset();
            authTcs.SetResult(CreateAuthenticationState("Monsieur").Result);

            // We need to wait here because the continuations of SetResult will be scheduled to run asynchronously.
            @event.Wait(Timeout);

            Assert.Equal(2, renderer.Batches.Count);
            var batch2 = renderer.Batches[1];
            var diff2 = batch2.DiffsByComponentId[authorizeViewComponentId].Single();
            Assert.Collection(diff2.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                Assert.Equal(0, edit.SiblingIndex);
                AssertFrame.Text(
                    batch2.ReferenceFrames[edit.ReferenceFrameIndex],
                    "Hello, Monsieur!");
            });

            // Assert: The IAuthorizationService was given expected criteria
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Monsieur", call.user.Identity.Name);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements,
                    req => Assert.IsType<DenyAnonymousAuthorizationRequirement>(req));
            });
        }

        [Fact]
        public void IncludesPolicyInAuthorizeCall()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView(policy: "MyTestPolicy");
            rootComponent.AuthenticationState = CreateAuthenticationState("Nellie");

            // Act
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();

            // Assert
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Nellie", call.user.Identity.Name);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements,
                    req => Assert.Equal("MyTestPolicy", ((TestPolicyRequirement)req).PolicyName));
            });
        }

        [Fact]
        public void IncludesRolesInAuthorizeCall()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = WrapInAuthorizeView(roles: "SuperTestRole1, SuperTestRole2");
            rootComponent.AuthenticationState = CreateAuthenticationState("Nellie");

            // Act
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();

            // Assert
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Nellie", call.user.Identity.Name);
                Assert.Null(call.resource);
                Assert.Collection(call.requirements, req => Assert.Equal(
                    new[] { "SuperTestRole1", "SuperTestRole2" },
                    ((RolesAuthorizationRequirement)req).AllowedRoles));
            });
        }

        [Fact]
        public void IncludesResourceInAuthorizeCall()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            var renderer = CreateTestRenderer(authorizationService);
            var resource = new object();
            var rootComponent = WrapInAuthorizeView(resource: resource);
            rootComponent.AuthenticationState = CreateAuthenticationState("Nellie");

            // Act
            renderer.AssignRootComponentId(rootComponent);
            rootComponent.TriggerRender();

            // Assert
            Assert.Collection(authorizationService.AuthorizeCalls, call =>
            {
                Assert.Equal("Nellie", call.user.Identity.Name);
                Assert.Same(resource, call.resource);
                Assert.Collection(call.requirements, req =>
                    Assert.IsType<DenyAnonymousAuthorizationRequirement>(req));
            });
        }

        [Fact]
        public void RejectsNonemptyScheme()
        {
            // Arrange
            var authorizationService = new TestAuthorizationService();
            var renderer = CreateTestRenderer(authorizationService);
            var rootComponent = new TestAuthStateProviderComponent(builder =>
            {
                builder.OpenComponent<AuthorizeViewCoreWithScheme>(0);
                builder.CloseComponent();
            });
            renderer.AssignRootComponentId(rootComponent);

            // Act/Assert
            var ex = Assert.Throws<NotSupportedException>(rootComponent.TriggerRender);
            Assert.Equal("The authorization data specifies an authentication scheme with value 'test scheme'. Authentication schemes cannot be specified for components.", ex.Message);
        }

        private static TestAuthStateProviderComponent WrapInAuthorizeView(
            RenderFragment<AuthenticationState> childContent = null,
            RenderFragment<AuthenticationState> authorized = null,
            RenderFragment<AuthenticationState> notAuthorized = null,
            RenderFragment authorizing = null,
            string policy = null,
            string roles = null,
            object resource = null)
        {
            return new TestAuthStateProviderComponent(builder =>
            {
                builder.OpenComponent<AuthorizeView>(0);
                builder.AddAttribute(1, nameof(AuthorizeView.ChildContent), childContent);
                builder.AddAttribute(2, nameof(AuthorizeView.Authorized), authorized);
                builder.AddAttribute(3, nameof(AuthorizeView.NotAuthorized), notAuthorized);
                builder.AddAttribute(4, nameof(AuthorizeView.Authorizing), authorizing);
                builder.AddAttribute(5, nameof(AuthorizeView.Policy), policy);
                builder.AddAttribute(6, nameof(AuthorizeView.Roles), roles);
                builder.AddAttribute(7, nameof(AuthorizeView.Resource), resource);
                builder.CloseComponent();
            });
        }

        class TestAuthStateProviderComponent : AutoRenderComponent
        {
            private readonly RenderFragment _childContent;

            public Task<AuthenticationState> AuthenticationState { get; set; }
                = Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

            public TestAuthStateProviderComponent(RenderFragment childContent)
            {
                _childContent = childContent;
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<Task<AuthenticationState>>>(0);
                builder.AddAttribute(1, nameof(CascadingValue<Task<AuthenticationState>>.Value), AuthenticationState);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(builder =>
                {
                    builder.OpenComponent<NeverReRenderComponent>(0);
                    builder.AddAttribute(1, "ChildContent", _childContent);
                    builder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        // This is useful to show that the reason why a CascadingValue refreshes is because the
        // value itself changed, not just that we're re-rendering the entire tree and have to
        // recurse into all descendants because we're passing ChildContent
        class NeverReRenderComponent : ComponentBase
        {
            [Parameter] public RenderFragment ChildContent { get; set; }

            protected override bool ShouldRender() => false;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, ChildContent);
            }
        }

        public static Task<AuthenticationState> CreateAuthenticationState(string username)
            => Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new TestIdentity { Name = username })));

        public TestRenderer CreateTestRenderer(IAuthorizationService authorizationService)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(authorizationService);
            serviceCollection.AddSingleton<IAuthorizationPolicyProvider>(new TestAuthorizationPolicyProvider());
            return new TestRenderer(serviceCollection.BuildServiceProvider());
        }

        public class AuthorizeViewCoreWithScheme : AuthorizeViewCore
        {
            protected override IAuthorizeData[] GetAuthorizeData()
                => new[] { new AuthorizeAttribute { AuthenticationSchemes = "test scheme" } };
        }
    }
}
