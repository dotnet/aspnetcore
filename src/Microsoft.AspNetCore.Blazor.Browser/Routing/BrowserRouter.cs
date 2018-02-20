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
    public class BrowserRouter : IComponent, IDisposable
    {
        RenderHandle _renderHandle;
        string _baseUriPrefix;
        string _locationAbsolute;

        public Assembly AppAssembly { get; set; }

        public string PagesNamespace { get; set; }

        public string DefaultComponentName { get; set; } = "Index";

        public void Init(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;

            UriHelper.EnableNavigationInteception();
            UriHelper.OnLocationChanged += OnLocationChanged;
            _baseUriPrefix = UriHelper.GetBaseUriPrefix();
            _locationAbsolute = UriHelper.GetAbsoluteUri();
        }

        public void SetParameters(ParameterCollection parameters)
        {
            parameters.AssignToProperties(this);
            Refresh();
        }

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

            var componentTypeName = $"{PagesNamespace}{locationPath.Replace('/', '.')}";
            if (componentTypeName[componentTypeName.Length - 1] == '.')
            {
                componentTypeName += DefaultComponentName;
            }

            return FindComponentTypeInAssemblyOrReferences(AppAssembly, componentTypeName)
                ?? throw new InvalidOperationException($"{nameof(BrowserRouter)} cannot find any component type with name {componentTypeName}.");
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
