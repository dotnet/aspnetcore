// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Layouts;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Browser.Routing
{
    /// <summary>
    /// A component that displays whichever other component corresponds to the
    /// browser's changing navigation state.
    /// </summary>
    public class BrowserRouter : IComponent, IDisposable
    {
        static readonly char[] _queryOrHashStartChar = new[] { '?', '#' };

        RenderHandle _renderHandle;
        string _baseUriPrefix;
        string _locationAbsolute;

        /// <summary>
        /// Gets or sets the assembly that should be searched, along with its referenced
        /// assemblies, for components matching the URI.
        /// </summary>
        public Assembly AppAssembly { get; set; }

        /// <summary>
        /// Gets or sets the namespace prefix that should be prepended when searching
        /// for matching components.
        /// </summary>
        public string PagesNamespace { get; set; }

        /// <summary>
        /// Gets or sets the component name that will be used if the URI ends with
        /// a slash.
        /// </summary>
        public string DefaultComponentName { get; set; } = "Index";

        /// <inheritdoc />
        public void Init(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;

            UriHelper.EnableNavigationInteception();
            UriHelper.OnLocationChanged += OnLocationChanged;
            _baseUriPrefix = UriHelper.GetBaseUriPrefix();
            _locationAbsolute = UriHelper.GetAbsoluteUri();
        }

        /// <inheritdoc />
        public void SetParameters(ParameterCollection parameters)
        {
            parameters.AssignToProperties(this);
            Refresh();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            UriHelper.OnLocationChanged -= OnLocationChanged;
        }

        protected virtual Type GetComponentTypeForPath(string locationPath)
        {
            if (AppAssembly == null)
            {
                throw new InvalidOperationException($"No value was specified for {nameof(AppAssembly)}.");
            }

            if (string.IsNullOrEmpty(PagesNamespace))
            {
                throw new InvalidOperationException($"No value was specified for {nameof(PagesNamespace)}.");
            }

            locationPath = StringUntilAny(locationPath, _queryOrHashStartChar);
            var componentTypeName = $"{PagesNamespace}{locationPath.Replace('/', '.')}";
            if (componentTypeName[componentTypeName.Length - 1] == '.')
            {
                componentTypeName += DefaultComponentName;
            }

            return FindComponentTypeInAssemblyOrReferences(AppAssembly, componentTypeName)
                ?? throw new InvalidOperationException($"{nameof(BrowserRouter)} cannot find any component type with name {componentTypeName}.");
        }

        private string StringUntilAny(string str, char[] chars)
        {
            var firstIndex = str.IndexOfAny(chars);
            return firstIndex < 0
                ? str
                : str.Substring(0, firstIndex);
        }

        private Type FindComponentTypeInAssemblyOrReferences(Assembly assembly, string typeName)
            => assembly.GetType(typeName, throwOnError: false, ignoreCase: true)
            ?? assembly.GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Select(referencedAssembly => FindComponentTypeInAssemblyOrReferences(referencedAssembly, typeName))
                .FirstOrDefault();

        protected virtual void Render(RenderTreeBuilder builder, Type matchedComponentType)
        {
            builder.OpenComponent(0, typeof(LayoutDisplay));
            builder.AddAttribute(1, nameof(LayoutDisplay.Page), matchedComponentType);
            builder.CloseComponent();
        }

        private void Refresh()
        {
            var locationPath = UriHelper.ToBaseRelativePath(_baseUriPrefix, _locationAbsolute);
            var matchedComponentType = GetComponentTypeForPath(locationPath);
            if (!typeof(IComponent).IsAssignableFrom(matchedComponentType))
            {
                throw new InvalidOperationException($"The type {matchedComponentType.FullName} " +
                    $"does not implement {typeof(IComponent).FullName}.");
            }

            _renderHandle.Render(builder => Render(builder, matchedComponentType));
        }

        private void OnLocationChanged(object sender, string newAbsoluteUri)
        {
            _locationAbsolute = newAbsoluteUri;
            if (_renderHandle.IsInitialized)
            {
                Refresh();
            }
        }
    }
}
