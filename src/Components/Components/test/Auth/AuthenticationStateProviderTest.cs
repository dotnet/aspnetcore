// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.Auth
{
    public class AuthenticationStateProviderTest
    {
        [Fact]
        public void RequiresRegisteredService()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new AutoRenderFragmentComponent(builder =>
            {
                builder.OpenComponent<AuthenticationStateProvider>(0);
                builder.CloseComponent();
            });

            // Act/Assert
            renderer.AssignRootComponentId(component);
            var ex = Assert.Throws<InvalidOperationException>(() => component.TriggerRender());
            Assert.Contains($"There is no registered service of type '{typeof(IAuthenticationStateProvider).FullName}'.", ex.Message);
        }

        [Fact]
        public void SuppliesSynchronouslyAvailableAuthStateToChildContent()
        {
            // Arrange: Service
            var services = new ServiceCollection();
            var authStateProvider = new TestAuthStateProvider()
            {
                CurrentAuthStateTask = Task.FromResult<IAuthenticationState>(new TestAuthState("Bert"))
            };
            services.AddSingleton<IAuthenticationStateProvider>(authStateProvider);

            // Arrange: Renderer and component
            var renderer = new TestRenderer(services.BuildServiceProvider());
            var component = new UseAuthenticationStateProviderComponent();

            // Act
            renderer.AssignRootComponentId(component);
            component.TriggerRender();

            // Assert
            var batch = renderer.Batches.Single();
            var receiveAuthStateId = batch.GetComponentFrames<ReceiveAuthStateComponent>().Single().ComponentId;
            var receiveAuthStateDiff = batch.DiffsByComponentId[receiveAuthStateId].Single();
            Assert.Collection(receiveAuthStateDiff.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    batch.ReferenceFrames[edit.ReferenceFrameIndex],
                    "Authenticated: True; Name: Bert; Pending: False; Renders: 1");
            });
        }

        [Fact]
        public void SuppliesAsynchronouslyAvailableAuthStateToChildContent()
        {
            // Arrange: Service
            var services = new ServiceCollection();
            var authStateTaskCompletionSource = new TaskCompletionSource<IAuthenticationState>();
            var authStateProvider = new TestAuthStateProvider()
            {
                CurrentAuthStateTask = authStateTaskCompletionSource.Task
            };
            services.AddSingleton<IAuthenticationStateProvider>(authStateProvider);

            // Arrange: Renderer and component
            var renderer = new TestRenderer(services.BuildServiceProvider());
            var component = new UseAuthenticationStateProviderComponent();

            // Act 1: Initial synchronous render
            renderer.AssignRootComponentId(component);
            component.TriggerRender();

            // Assert 1: Empty state
            var batch1 = renderer.Batches.Single();
            var receiveAuthStateId = batch1.GetComponentFrames<ReceiveAuthStateComponent>().Single().ComponentId;
            var receiveAuthStateDiff1 = batch1.DiffsByComponentId[receiveAuthStateId].Single();
            Assert.Collection(receiveAuthStateDiff1.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    batch1.ReferenceFrames[edit.ReferenceFrameIndex],
                    "Authenticated: False; Name: ; Pending: True; Renders: 1");
            });

            // Act 2: Auth state fetch task completes in background
            authStateTaskCompletionSource.SetResult(new TestAuthState("Bert"));

            // Assert 2: Re-renders content
            Assert.Equal(2, renderer.Batches.Count);
            var batch2 = renderer.Batches.Last();
            var receiveAuthStateDiff2 = batch2.DiffsByComponentId[receiveAuthStateId].Single();
            Assert.Collection(receiveAuthStateDiff2.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                AssertFrame.Text(
                    batch2.ReferenceFrames[edit.ReferenceFrameIndex],
                    "Authenticated: True; Name: Bert; Pending: False; Renders: 2");
            });
        }

        [Fact]
        public void RespondsToNotificationsFromAuthenticationStateProvider()
        {
            // Arrange: Service
            var services = new ServiceCollection();
            var authStateProvider = new TestAuthStateProvider()
            {
                CurrentAuthStateTask = Task.FromResult<IAuthenticationState>(new TestAuthState(null))
            };
            services.AddSingleton<IAuthenticationStateProvider>(authStateProvider);

            // Arrange: Renderer and component, initially rendered
            var renderer = new TestRenderer(services.BuildServiceProvider());
            var component = new UseAuthenticationStateProviderComponent();
            renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var receiveAuthStateId = renderer.Batches.Single()
                .GetComponentFrames<ReceiveAuthStateComponent>().Single().ComponentId;

            // Act 2: AuthenticationStateProvider issues notification
            authStateProvider.TriggerAuthenticationStateChanged(new TestAuthState("Bert"));

            // Assert 2: Re-renders content
            Assert.Equal(2, renderer.Batches.Count);
            var batch = renderer.Batches.Last();
            var receiveAuthStateDiff = batch.DiffsByComponentId[receiveAuthStateId].Single();
            Assert.Collection(receiveAuthStateDiff.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                AssertFrame.Text(
                    batch.ReferenceFrames[edit.ReferenceFrameIndex],
                    "Authenticated: True; Name: Bert; Pending: False; Renders: 2");
            });
        }

        class ReceiveAuthStateComponent : AutoRenderComponent
        {
            int numRenders;

            [CascadingParameter] IAuthenticationState AuthState { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                numRenders++;
                var identity = AuthState.User.Identity;
                builder.AddContent(0, $"Authenticated: {identity.IsAuthenticated}; Name: {identity.Name}; Pending: {AuthState.IsPending}; Renders: {numRenders}");
            }
        }

        class UseAuthenticationStateProviderComponent : AutoRenderComponent
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<AuthenticationStateProvider>(0);
                builder.AddAttribute(1, RenderTreeBuilder.ChildContent, new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<ReceiveAuthStateComponent>(0);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        class TestAuthStateProvider : IAuthenticationStateProvider
        {
            public Task<IAuthenticationState> CurrentAuthStateTask { get; set; }

#pragma warning disable 0067 // "Never used"
            public event AuthenticationStateChangedHandler AuthenticationStateChanged;
#pragma warning restore 0067 // "Never used"

            public Task<IAuthenticationState> GetAuthenticationStateAsync(bool forceRefresh)
            {
                return CurrentAuthStateTask;
            }

            internal void TriggerAuthenticationStateChanged(TestAuthState authState)
            {
                AuthenticationStateChanged?.Invoke(authState);
            }
        }

        class TestAuthState : IAuthenticationState
        {
            public TestAuthState(string usernameOrNull)
            {
                User = new ClaimsPrincipal(usernameOrNull == null
                    ? (IIdentity)new ClaimsIdentity()
                    : new TestIdentity { Name = usernameOrNull });
            }

            public ClaimsPrincipal User { get; }

            public bool IsPending => false;
        }

        class TestIdentity : IIdentity
        {
            public string AuthenticationType => "Test";

            public bool IsAuthenticated => true;

            public string Name { get; set; }
        }
    }
}
