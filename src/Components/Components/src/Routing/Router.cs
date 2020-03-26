// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// A component that supplies route data corresponding to the current navigation state.
    /// </summary>
    public class Router : IComponent, IHandleAfterRender, IDisposable
    {
        static readonly char[] _queryOrHashStartChar = new[] { '?', '#' };
        static readonly ReadOnlyDictionary<string, object> _emptyParametersDictionary
            = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        RenderHandle _renderHandle;
        string _baseUri;
        string _locationAbsolute;
        bool _navigationInterceptionEnabled;
        ILogger<Router> _logger;

        [Inject] private NavigationManager NavigationManager { get; set; }

        [Inject] private INavigationInterception NavigationInterception { get; set; }

        [Inject] private ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        /// Gets or sets the assembly that should be searched for components matching the URI.
        /// </summary>
        [Parameter] public Assembly AppAssembly { get; set; }

        /// <summary>
        /// Gets or sets a collection of additional assemblies that should be searched for components
        /// that can match URIs.
        /// </summary>
        [Parameter] public IEnumerable<Assembly> AdditionalAssemblies { get; set; }

        /// <summary>
        /// Gets or sets the content to display when no match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment NotFound { get; set; }

        /// <summary>
        /// Gets or sets the content to display when a match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment<RouteData> Found { get; set; }

        private RouteTable Routes { get; set; }

        /// <inheritdoc />
        public void Attach(RenderHandle renderHandle)
        {
            _logger = LoggerFactory.CreateLogger<Router>();
            _renderHandle = renderHandle;
            _baseUri = NavigationManager.BaseUri;
            _locationAbsolute = NavigationManager.Uri;
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            if (AppAssembly == null)
            {
                throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(AppAssembly)}.");
            }

            // Found content is mandatory, because even though we could use something like <RouteView ...> as a
            // reasonable default, if it's not declared explicitly in the template then people will have no way
            // to discover how to customize this (e.g., to add authorization).
            if (Found == null)
            {
                throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(Found)}.");
            }

            // NotFound content is mandatory, because even though we could display a default message like "Not found",
            // it has to be specified explicitly so that it can also be wrapped in a specific layout
            if (NotFound == null)
            {
                throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(NotFound)}.");
            }


            var assemblies = AdditionalAssemblies == null ? new[] { AppAssembly } : new[] { AppAssembly }.Concat(AdditionalAssemblies);
            Routes = RouteTableFactory.Create(assemblies);
            Refresh(isNavigationIntercepted: false);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        private static string StringUntilAny(string str, char[] chars)
        {
            var firstIndex = str.IndexOfAny(chars);
            return firstIndex < 0
                ? str
                : str.Substring(0, firstIndex);
        }

        private void Refresh(bool isNavigationIntercepted)
        {
            var locationPath = NavigationManager.ToBaseRelativePath(_locationAbsolute);
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

                Log.NavigatingToComponent(_logger, context.Handler, locationPath, _baseUri);

                var routeData = new RouteData(
                    context.Handler,
                    context.Parameters ?? _emptyParametersDictionary);
                _renderHandle.Render(Found(routeData));
            }
            else
            {
                if (!isNavigationIntercepted)
                {
                    Log.DisplayingNotFound(_logger, locationPath, _baseUri);

                    // We did not find a Component that matches the route.
                    // Only show the NotFound content if the application developer programatically got us here i.e we did not
                    // intercept the navigation. In all other cases, force a browser navigation since this could be non-Blazor content.
                    _renderHandle.Render(NotFound);
                }
                else
                {
                    Log.NavigatingToExternalUri(_logger, _locationAbsolute, locationPath, _baseUri);
                    NavigationManager.NavigateTo(_locationAbsolute, forceLoad: true);
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
            if (!_navigationInterceptionEnabled)
            {
                _navigationInterceptionEnabled = true;
                return NavigationInterception.EnableNavigationInterceptionAsync();
            }

            return Task.CompletedTask;
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, string, Exception> _displayingNotFound =
                LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(1, "DisplayingNotFound"), $"Displaying {nameof(NotFound)} because path '{{Path}}' with base URI '{{BaseUri}}' does not match any component route");

            private static readonly Action<ILogger, Type, string, string, Exception> _navigatingToComponent =
                LoggerMessage.Define<Type, string, string>(LogLevel.Debug, new EventId(2, "NavigatingToComponent"), "Navigating to component {ComponentType} in response to path '{Path}' with base URI '{BaseUri}'");

            private static readonly Action<ILogger, string, string, string, Exception> _navigatingToExternalUri =
                LoggerMessage.Define<string, string, string>(LogLevel.Debug, new EventId(3, "NavigatingToExternalUri"), "Navigating to non-component URI '{ExternalUri}' in response to path '{Path}' with base URI '{BaseUri}'");

            internal static void DisplayingNotFound(ILogger logger, string path, string baseUri)
            {
                _displayingNotFound(logger, path, baseUri, null);
            }

            internal static void NavigatingToComponent(ILogger logger, Type componentType, string path, string baseUri)
            {
                _navigatingToComponent(logger, componentType, path, baseUri, null);
            }

            internal static void NavigatingToExternalUri(ILogger logger, string externalUri, string path, string baseUri)
            {
                _navigatingToExternalUri(logger, externalUri, path, baseUri, null);
            }
        }
    }
}
