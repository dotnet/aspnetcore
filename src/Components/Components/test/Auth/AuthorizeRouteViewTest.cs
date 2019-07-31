// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class AuthorizeRouteViewTest
    {
        private readonly static IReadOnlyDictionary<string, object> EmptyParametersDictionary = new Dictionary<string, object>();
        private readonly TestAuthenticationStateProvider _authenticationStateProvider;
        private readonly TestRenderer _renderer;
        private readonly RouteView _authorizeRouteViewComponent;
        private readonly int _authorizeRouteViewComponentId;

        public AuthorizeRouteViewTest()
        {
            _authenticationStateProvider = new TestAuthenticationStateProvider();
            _authenticationStateProvider.CurrentAuthStateTask = Task.FromResult(
                new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<AuthenticationStateProvider>(_authenticationStateProvider);
            serviceCollection.AddSingleton<IAuthorizationPolicyProvider, TestAuthorizationPolicyProvider>();
            serviceCollection.AddSingleton<IAuthorizationService, TestAuthorizationService>();

            _renderer = new TestRenderer(serviceCollection.BuildServiceProvider());
            _authorizeRouteViewComponent = new AuthorizeRouteView();
            _authorizeRouteViewComponentId = _renderer.AssignRootComponentId(_authorizeRouteViewComponent);
        }

        [Fact]
        public void WithoutCascadedAuthenticationState_WrapsOutputInCascadingAuthenticationState()
        {
            // Arrange/Act
            var routeData = new ComponentRouteData(typeof(TestPageComponent), EmptyParametersDictionary);
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
                component => Assert.True(component is AuthorizeViewCore),
                component => Assert.IsType<LayoutView>(component),
                component => Assert.IsType<TestPageComponent>(component));
        }

        [Fact]
        public void WithCascadedAuthenticationState_DoesNotWrapOutputInCascadingAuthenticationState()
        {
            // Arrange
            var routeData = new ComponentRouteData(typeof(TestPageComponent), EmptyParametersDictionary);
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
                component => Assert.True(component is AuthorizeViewCore),
                component => Assert.IsType<LayoutView>(component),
                component => Assert.IsType<TestPageComponent>(component));
        }

        // Renders an AuthorizeViewCore subclass which honours the authentication result from the route data
        // When authorized, renders a layoutview with the page, parameters, and default layout
        // When authorizing, renders authorizing content in the default layout
        // When not authorized, renders notauthorized content in the default layout (with context)

        class TestPageComponent : ComponentBase { }

        class AuthorizeRouteViewWithExistingCascadedAuthenticationState : AutoRenderComponent
        {
            private readonly Task<AuthenticationState> _authenticationState;
            private readonly ComponentRouteData _routeData;

            public AuthorizeRouteViewWithExistingCascadedAuthenticationState(
                Task<AuthenticationState> authenticationState,
                ComponentRouteData routeData)
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
