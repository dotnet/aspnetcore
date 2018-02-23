// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Browser.Routing
{
    // TODO: Move this into Microsoft.AspNetCore.Blazor, and use DI to break the
    // coupling on Microsoft.AspNetCore.Blazor.Browser.Routing.UriHelper.
    // That's because you'd use NavLink in non-browser scenarios too (e.g., prerendering).
    // Can't do this until DI is implemented.

    // NOTE: This could be implemented in a more performant way by iterating through
    // the ParameterCollection only once (instead of multiple TryGetValue calls), and
    // avoiding allocating a dictionary in the case where there are no additional params.
    // However the intention here is to get a sense of what more high-level coding patterns
    // will exist and what APIs are needed to support them. Later in the project when we
    // have more examples of components implemented in pure C# (not Razor) we could change
    // this one to the more low-level perf-sensitive implementation.

    public class NavLink : IComponent, IDisposable
    {
        private RenderHandle _renderHandle;
        private bool _isActive;

        private RenderFragment _childContent;
        private string _cssClass;
        private string _hrefAbsolute;
        private IReadOnlyDictionary<string, object> _allAttributes;

        [Inject] private IUriHelper UriHelper { get; set; }

        public void Init(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;

            // We'll consider re-rendering on each location change
            UriHelper.OnLocationChanged += OnLocationChanged;
        }

        public void SetParameters(ParameterCollection parameters)
        {
            // Capture the parameters we want to do special things with, plus all as a dictionary
            parameters.TryGetValue(RenderTreeBuilder.ChildContent, out _childContent);
            parameters.TryGetValue("class", out _cssClass);
            parameters.TryGetValue("href", out string href);
            _allAttributes = parameters.ToDictionary();

            // Update computed state and render
            _hrefAbsolute = href == null ? null : UriHelper.ToAbsoluteUri(href).AbsoluteUri;
            _isActive = UriHelper.GetAbsoluteUri().Equals(_hrefAbsolute, StringComparison.Ordinal);
            _renderHandle.Render(Render);
        }

        public void Dispose()
        {
            // To avoid leaking memory, it's important to detach any event handlers in Dispose()
            UriHelper.OnLocationChanged -= OnLocationChanged;
        }

        private void OnLocationChanged(object sender, string newUri)
        {
            // We could just re-render always, but for this component we know the
            // only relevant state change is to the _isActive property.
            var shouldBeActiveNow = newUri.Equals(_hrefAbsolute, StringComparison.Ordinal);
            if (shouldBeActiveNow != _isActive)
            {
                _isActive = shouldBeActiveNow;
                _renderHandle.Render(Render);
            }
        }

        private void Render(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "a");

            // Set "active" class dynamically
            builder.AddAttribute(0, "class", CombineWithSpace(_cssClass, _isActive ? "active" : null));

            // Pass through all other attributes unchanged
            foreach (var kvp in _allAttributes.Where(kvp => kvp.Key != "class"))
            {
                builder.AddAttribute(0, kvp.Key, kvp.Value);
            }

            // Pass through any child content unchanged
            builder.AddContent(1, _childContent);

            builder.CloseElement();
        }

        private string CombineWithSpace(string str1, string str2)
            => str1 == null ? str2
            : (str2 == null ? str1 : $"{str1} {str2}");
    }
}
