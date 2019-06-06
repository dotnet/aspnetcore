// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Layouts;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// A component that displays whichever other component corresponds to the
    /// current navigation location.
    /// </summary>
    public class Router : IComponent, IHandleAfterRender, IDisposable
    {
        static readonly char[] _queryOrHashStartChar = new[] { '?', '#' };

        RenderHandle _renderHandle;
        string _baseUri;
        string _locationAbsolute;
        bool _navigationInterceptionEnabled;

        [Inject] private IUriHelper UriHelper { get; set; }

        [Inject] private INavigationInterception NavigationInterception { get; set; }

        [Inject] private IComponentContext ComponentContext { get; set; }

        /// <summary>
        /// Gets or sets the assembly that should be searched, along with its referenced
        /// assemblies, for components matching the URI.
        /// </summary>
        [Parameter] public Assembly AppAssembly { get; private set; }
        
        /// <summary>
        /// Gets or sets the type of the component that should be used as a fallback when no match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment NotFoundContent { get; private set; }

        /// <summary>
        /// The content that will be displayed if the user is not authorized.
        /// </summary>
        [Parameter] public RenderFragment<AuthenticationState> NotAuthorizedContent { get; private set; }

        /// <summary>
        /// The content that will be displayed while asynchronous authorization is in progress.
        /// </summary>
        [Parameter] public RenderFragment AuthorizingContent { get; private set; }

        private RouteTable Routes { get; set; }

        /// <inheritdoc />
        public void Configure(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
            _baseUri = UriHelper.GetBaseUri();
            _locationAbsolute = UriHelper.GetAbsoluteUri();
            UriHelper.OnLocationChanged += OnLocationChanged;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);
            var types = ComponentResolver.ResolveComponents(AppAssembly);
            Routes = RouteTable.Create(types);
            Refresh(isNavigationIntercepted: false);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            UriHelper.OnLocationChanged -= OnLocationChanged;
        }

        private string StringUntilAny(string str, char[] chars)
        {
            var firstIndex = str.IndexOfAny(chars);
            return firstIndex < 0
                ? str
                : str.Substring(0, firstIndex);
        }

        /// <inheritdoc />
        protected virtual void Render(RenderTreeBuilder builder, Type handler, IDictionary<string, object> parameters)
        {
            builder.OpenComponent(0, typeof(PageDisplay));
            builder.AddAttribute(1, nameof(PageDisplay.Page), handler);
            builder.AddAttribute(2, nameof(PageDisplay.PageParameters), parameters);
            builder.AddAttribute(3, nameof(PageDisplay.NotAuthorizedContent), NotAuthorizedContent);
            builder.AddAttribute(4, nameof(PageDisplay.AuthorizingContent), AuthorizingContent);
            builder.CloseComponent();
        }

        private void Refresh(bool isNavigationIntercepted)
        {
            var locationPath = UriHelper.ToBaseRelativePath(_baseUri, _locationAbsolute);
            locationPath = StringUntilAny(locationPath, _queryOrHashStartChar);
            var context = new RouteContext(locationPath);
            Routes.Route(context);

            if (context.Handler != null)
            {
                if (!typeof(IComponent).IsAssignableFrom(context.Handler))
                {
                    throw new InvalidOperationException($"The type {context.Handler.FullName} " +
                        $"does not implement {typeof(IComponent).FullName}.");
                }

                _renderHandle.Render(builder => Render(builder, context.Handler, context.Parameters));
            }
            else
            {
                if (!isNavigationIntercepted && NotFoundContent != null)
                {
                    // We did not find a Component that matches the route.
                    // Only show the NotFoundContent if the application developer programatically got us here i.e we did not
                    // intercept the navigation. In all other cases, force a browser navigation since this could be non-Blazor content.
                    _renderHandle.Render(NotFoundContent);
                }
                else
                {
                    UriHelper.NavigateTo(_locationAbsolute, forceLoad: true);
                }
            }
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            _locationAbsolute = args.Location;
            if (_renderHandle.IsInitialized && Routes != null)
            {
                Refresh(args.IsNavigationIntercepted);
            }
        }

        Task IHandleAfterRender.OnAfterRenderAsync()
        {
            if (!_navigationInterceptionEnabled && ComponentContext.IsConnected)
            {
                _navigationInterceptionEnabled = true;
                return NavigationInterception.EnableNavigationInterceptionAsync();
            }

            return Task.CompletedTask;
        }
    }
}
