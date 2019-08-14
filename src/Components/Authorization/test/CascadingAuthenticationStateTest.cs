// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.Authorization
{
    public class CascadingAuthenticationStateTest
    {
        [Fact]
        public void RequiresRegisteredService()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new AutoRenderFragmentComponent(builder =>
            {
                builder.OpenComponent<CascadingAuthenticationState>(0);
                builder.CloseComponent();
            });

            // Act/Assert
            renderer.AssignRootComponentId(component);
            var ex = Assert.Throws<InvalidOperationException>(() => component.TriggerRender());
            Assert.Contains($"There is no registered service of type '{typeof(AuthenticationStateProvider).FullName}'.", ex.Message);
        }

        [Fact]
        public void SuppliesSynchronouslyAvailableAuthStateToChildContent()
        {
            // Arrange: Service
            var services = new ServiceCollection();
            var authStateProvider = new TestAuthenticationStateProvider()
            {
                CurrentAuthStateTask = Task.FromResult(CreateAuthenticationState("Bert"))
            };
            services.AddSingleton<AuthenticationStateProvider>(authStateProvider);

            // Arrange: Renderer and component
            var renderer = new TestRenderer(services.BuildServiceProvider());
            var component = new UseCascadingAuthenticationStateComponent();

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
            var authStateTaskCompletionSource = new TaskCompletionSource<AuthenticationState>();
            var authStateProvider = new TestAuthenticationStateProvider()
            {
                CurrentAuthStateTask = authStateTaskCompletionSource.Task
            };
            services.AddSingleton<AuthenticationStateProvider>(authStateProvider);

            // Arrange: Renderer and component
            var renderer = new TestRenderer(services.BuildServiceProvider());
            var component = new UseCascadingAuthenticationStateComponent();

            // Act 1: Initial synchronous render
            renderer.AssignRootComponentId(component);
            component.TriggerRender();

            // Assert 1: Empty state
            var batch1 = renderer.Batches.Single();
            var receiveAuthStateFrame = batch1.GetComponentFrames<ReceiveAuthStateComponent>().Single();
            var receiveAuthStateId = receiveAuthStateFrame.ComponentId;
            var receiveAuthStateComponent = (ReceiveAuthStateComponent)receiveAuthStateFrame.Component;
            var receiveAuthStateDiff1 = batch1.DiffsByComponentId[receiveAuthStateId].Single();
            Assert.Collection(receiveAuthStateDiff1.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Text(
                    batch1.ReferenceFrames[edit.ReferenceFrameIndex],
                    "Authenticated: False; Name: ; Pending: True; Renders: 1");
            });

            // Act/Assert 2: Auth state fetch task completes in background
            // No new renders yet, because the cascading parameter itself hasn't changed
            authStateTaskCompletionSource.SetResult(CreateAuthenticationState("Bert"));
            Assert.Single(renderer.Batches);

            // Act/Assert 3: Refresh display
            receiveAuthStateComponent.TriggerRender();
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
            var authStateProvider = new TestAuthenticationStateProvider()
            {
                CurrentAuthStateTask = Task.FromResult(CreateAuthenticationState(null))
            };
            services.AddSingleton<AuthenticationStateProvider>(authStateProvider);

            // Arrange: Renderer and component, initially rendered
            var renderer = new TestRenderer(services.BuildServiceProvider());
            var component = new UseCascadingAuthenticationStateComponent();
            renderer.AssignRootComponentId(component);
            component.TriggerRender();
            var receiveAuthStateId = renderer.Batches.Single()
                .GetComponentFrames<ReceiveAuthStateComponent>().Single().ComponentId;

            // Act 2: AuthenticationStateProvider issues notification
            authStateProvider.TriggerAuthenticationStateChanged(
                Task.FromResult(CreateAuthenticationState("Bert")));

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

            [CascadingParameter] Task<AuthenticationState> AuthStateTask { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                numRenders++;

                if (AuthStateTask.IsCompleted)
                {
                    var identity = AuthStateTask.Result.User.Identity;
                    builder.AddContent(0, $"Authenticated: {identity.IsAuthenticated}; Name: {identity.Name}; Pending: False; Renders: {numRenders}");
                }
                else
                {
                    builder.AddContent(0, $"Authenticated: False; Name: ; Pending: True; Renders: {numRenders}");
                }
            }
        }

        class UseCascadingAuthenticationStateComponent : AutoRenderComponent
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingAuthenticationState>(0);
                builder.AddAttribute(1, "ChildContent", new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<ReceiveAuthStateComponent>(0);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        public static AuthenticationState CreateAuthenticationState(string username)
            => new AuthenticationState(new ClaimsPrincipal(username == null
                ? new ClaimsIdentity()
                : (IIdentity)new TestIdentity { Name = username }));

        class TestIdentity : IIdentity
        {
            public string AuthenticationType => "Test";

            public bool IsAuthenticated => true;

            public string Name { get; set; }
        }
    }
}
