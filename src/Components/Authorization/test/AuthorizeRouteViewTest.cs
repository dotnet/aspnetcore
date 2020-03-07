// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.Authorization
{
    public class AuthorizeRouteViewTest
    {
        private readonly static IReadOnlyDictionary<string, object> EmptyParametersDictionary = new Dictionary<string, object>();
        private readonly TestAuthenticationStateProvider _authenticationStateProvider;
        private readonly TestRenderer _renderer;
        private readonly RouteView _authorizeRouteViewComponent;
        private readonly int _authorizeRouteViewComponentId;
        private readonly TestAuthorizationService _testAuthorizationService;

        public AuthorizeRouteViewTest()
        {
            _authenticationStateProvider = new TestAuthenticationStateProvider();
            _authenticationStateProvider.CurrentAuthStateTask = Task.FromResult(
                new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

            _testAuthorizationService = new TestAuthorizationService();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<AuthenticationStateProvider>(_authenticationStateProvider);
            serviceCollection.AddSingleton<IAuthorizationPolicyProvider, TestAuthorizationPolicyProvider>();
            serviceCollection.AddSingleton<IAuthorizationService>(_testAuthorizationService);

            _renderer = new TestRenderer(serviceCollection.BuildServiceProvider());
            _authorizeRouteViewComponent = new AuthorizeRouteView();
            _authorizeRouteViewComponentId = _renderer.AssignRootComponentId(_authorizeRouteViewComponent);
        }

        [Fact]
        public void WhenAuthorized_RendersPageInsideLayout()
        {
            // Arrange
            var routeData = new RouteData(typeof(TestPageRequiringAuthorization), new Dictionary<string, object>
            {
                { nameof(TestPageRequiringAuthorization.Message), "Hello, world!" }
            });
            _testAuthorizationService.NextResult = AuthorizationResult.Success();

            // Act
            _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
            }));

            // Assert: renders layout
            var batch = _renderer.Batches.Single();
            var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
            Assert.Collection(layoutDiff.Edits,
                edit => AssertPrependText(batch, edit, "Layout starts here"),
                edit =>
                {
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    AssertFrame.Component<TestPageRequiringAuthorization>(batch.ReferenceFrames[edit.ReferenceFrameIndex]);
                },
                edit => AssertPrependText(batch, edit, "Layout ends here"));

            // Assert: renders page
            var pageDiff = batch.GetComponentDiffs<TestPageRequiringAuthorization>().Single();
            Assert.Collection(pageDiff.Edits,
                edit => AssertPrependText(batch, edit, "Hello from the page with message: Hello, world!"));
        }

        [Fact]
        public void WhenNotAuthorized_RendersDefaultNotAuthorizedContentInsideLayout()
        {
            // Arrange
            var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
            _testAuthorizationService.NextResult = AuthorizationResult.Failed();

            // Act
            _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
            }));

            // Assert: renders layout containing "not authorized" message
            var batch = _renderer.Batches.Single();
            var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
            Assert.Collection(layoutDiff.Edits,
                edit => AssertPrependText(batch, edit, "Layout starts here"),
                edit => AssertPrependText(batch, edit, "Not authorized"),
                edit => AssertPrependText(batch, edit, "Layout ends here"));
        }

        [Fact]
        public void WhenNotAuthorized_RendersCustomNotAuthorizedContentInsideLayout()
        {
            // Arrange
            var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
            _testAuthorizationService.NextResult = AuthorizationResult.Failed();
            _authenticationStateProvider.CurrentAuthStateTask = Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new TestIdentity { Name = "Bert" })));

            // Act
            RenderFragment<AuthenticationState> customNotAuthorized =
                state => builder => builder.AddContent(0, $"Go away, {state.User.Identity.Name}");
            _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.NotAuthorized), customNotAuthorized },
            }));

            // Assert: renders layout containing "not authorized" message
            var batch = _renderer.Batches.Single();
            var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
            Assert.Collection(layoutDiff.Edits,
                edit => AssertPrependText(batch, edit, "Layout starts here"),
                edit => AssertPrependText(batch, edit, "Go away, Bert"),
                edit => AssertPrependText(batch, edit, "Layout ends here"));
        }

        [Fact]
        public async Task WhenAuthorizing_RendersDefaultAuthorizingContentInsideLayout()
        {
            // Arrange
            var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
            var authStateTcs = new TaskCompletionSource<AuthenticationState>();
            _authenticationStateProvider.CurrentAuthStateTask = authStateTcs.Task;
            RenderFragment<AuthenticationState> customNotAuthorized =
                state => builder => builder.AddContent(0, $"Go away, {state.User.Identity.Name}");

            // Act
            var firstRenderTask = _renderer.RenderRootComponentAsync(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.NotAuthorized), customNotAuthorized },
            }));

            // Assert: renders layout containing "authorizing" message
            Assert.False(firstRenderTask.IsCompleted);
            var batch = _renderer.Batches.Single();
            var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
            Assert.Collection(layoutDiff.Edits,
                edit => AssertPrependText(batch, edit, "Layout starts here"),
                edit => AssertPrependText(batch, edit, "Authorizing..."),
                edit => AssertPrependText(batch, edit, "Layout ends here"));

            // Act 2: updates when authorization completes
            authStateTcs.SetResult(new AuthenticationState(
                new ClaimsPrincipal(new TestIdentity { Name = "Bert" })));
            await firstRenderTask;

            // Assert 2: Only the layout is updated
            batch = _renderer.Batches.Skip(1).Single();
            var nonEmptyDiff = batch.DiffsInOrder.Where(d => d.Edits.Any()).Single();
            Assert.Equal(layoutDiff.ComponentId, nonEmptyDiff.ComponentId);
            Assert.Collection(nonEmptyDiff.Edits, edit =>
            {
                Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
                Assert.Equal(1, edit.SiblingIndex);
                AssertFrame.Text(batch.ReferenceFrames[edit.ReferenceFrameIndex], "Go away, Bert");
            });
        }

        [Fact]
        public void WhenAuthorizing_RendersCustomAuthorizingContentInsideLayout()
        {
            // Arrange
            var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
            var authStateTcs = new TaskCompletionSource<AuthenticationState>();
            _authenticationStateProvider.CurrentAuthStateTask = authStateTcs.Task;
            RenderFragment customAuthorizing =
                builder => builder.AddContent(0, "Hold on, we're checking your papers.");

            // Act
            var firstRenderTask = _renderer.RenderRootComponentAsync(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.Authorizing), customAuthorizing },
            }));

            // Assert: renders layout containing "authorizing" message
            Assert.False(firstRenderTask.IsCompleted);
            var batch = _renderer.Batches.Single();
            var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
            Assert.Collection(layoutDiff.Edits,
                edit => AssertPrependText(batch, edit, "Layout starts here"),
                edit => AssertPrependText(batch, edit, "Hold on, we're checking your papers."),
                edit => AssertPrependText(batch, edit, "Layout ends here"));
        }

        [Fact]
        public void WithoutCascadedAuthenticationState_WrapsOutputInCascadingAuthenticationState()
        {
            // Arrange/Act
            var routeData = new RouteData(typeof(TestPageWithNoAuthorization), EmptyParametersDictionary);
            _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData }
            }));

            // Assert
            var batch = _renderer.Batches.Single();
            var componentInstances = batch.ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Component)
                .Select(f => f.Component);

            Assert.Collection(componentInstances,
                // This is the hierarchy inside the AuthorizeRouteView, which contains its
                // own CascadingAuthenticationState
                component => Assert.IsType<CascadingAuthenticationState>(component),
                component => Assert.IsType<CascadingValue<Task<AuthenticationState>>>(component),
                component => Assert.IsAssignableFrom<AuthorizeViewCore>(component),
                component => Assert.IsType<LayoutView>(component),
                component => Assert.IsType<TestPageWithNoAuthorization>(component));
        }

        [Fact]
        public void WithCascadedAuthenticationState_DoesNotWrapOutputInCascadingAuthenticationState()
        {
            // Arrange
            var routeData = new RouteData(typeof(TestPageWithNoAuthorization), EmptyParametersDictionary);
            var rootComponent = new AuthorizeRouteViewWithExistingCascadedAuthenticationState(
                _authenticationStateProvider.CurrentAuthStateTask,
                routeData);
            var rootComponentId = _renderer.AssignRootComponentId(rootComponent);

            // Act
            _renderer.RenderRootComponent(rootComponentId);

            // Assert
            var batch = _renderer.Batches.Single();
            var componentInstances = batch.ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Component)
                .Select(f => f.Component);

            Assert.Collection(componentInstances,
                // This is the externally-supplied cascading value
                component => Assert.IsType<CascadingValue<Task<AuthenticationState>>>(component),
                component => Assert.IsType<AuthorizeRouteView>(component),

                // This is the hierarchy inside the AuthorizeRouteView. It doesn't contain a
                // further CascadingAuthenticationState
                component => Assert.IsAssignableFrom<AuthorizeViewCore>(component),
                component => Assert.IsType<LayoutView>(component),
                component => Assert.IsType<TestPageWithNoAuthorization>(component));
        }

        [Fact]
        public void UpdatesOutputWhenRouteDataChanges()
        {
            // Arrange/Act 1: Start on some route
            // Not asserting about the initial output, as that is covered by other tests
            var routeData = new RouteData(typeof(TestPageWithNoAuthorization), EmptyParametersDictionary);
            _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
            }));

            // Act 2: Move to another route
            var routeData2 = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
            var render2Task = _renderer.Dispatcher.InvokeAsync(() => _authorizeRouteViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData2 },
            })));

            // Assert: we retain the layout instance, and mutate its contents
            Assert.True(render2Task.IsCompletedSuccessfully);
            Assert.Equal(2, _renderer.Batches.Count);
            var batch2 = _renderer.Batches[1];
            var diff = batch2.DiffsInOrder.Where(d => d.Edits.Any()).Single();
            Assert.Collection(diff.Edits,
                edit =>
                {
                    // Inside the layout, we add the new content
                    Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                    Assert.Equal(1, edit.SiblingIndex);
                    AssertFrame.Text(batch2.ReferenceFrames[edit.ReferenceFrameIndex], "Not authorized");
                },
                edit =>
                {
                    // ... and remove the old content
                    Assert.Equal(RenderTreeEditType.RemoveFrame, edit.Type);
                    Assert.Equal(2, edit.SiblingIndex);
                });
        }

        private static void AssertPrependText(CapturedBatch batch, RenderTreeEdit edit, string text)
        {
            Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
            ref var referenceFrame = ref batch.ReferenceFrames[edit.ReferenceFrameIndex];
            AssertFrame.Text(referenceFrame, text);
        }

        class TestPageWithNoAuthorization : ComponentBase { }

        [Authorize]
        class TestPageRequiringAuthorization : ComponentBase
        {
            [Parameter] public string Message { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, $"Hello from the page with message: {Message}");
            }
        }

        class TestLayout : LayoutComponentBase
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, "Layout starts here");
                builder.AddContent(1, Body);
                builder.AddContent(2, "Layout ends here");
            }
        }

        class AuthorizeRouteViewWithExistingCascadedAuthenticationState : AutoRenderComponent
        {
            private readonly Task<AuthenticationState> _authenticationState;
            private readonly RouteData _routeData;

            public AuthorizeRouteViewWithExistingCascadedAuthenticationState(
                Task<AuthenticationState> authenticationState,
                RouteData routeData)
            {
                _authenticationState = authenticationState;
                _routeData = routeData;
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<Task<AuthenticationState>>>(0);
                builder.AddAttribute(1, nameof(CascadingValue<object>.Value), _authenticationState);
                builder.AddAttribute(2, nameof(CascadingValue<object>.ChildContent), (RenderFragment)(builder =>
                {
                    builder.OpenComponent<AuthorizeRouteView>(0);
                    builder.AddAttribute(1, nameof(AuthorizeRouteView.RouteData), _routeData);
                    builder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }
    }
}
