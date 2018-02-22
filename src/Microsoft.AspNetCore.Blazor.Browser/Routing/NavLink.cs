// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Browser.Routing
{
    // TODO: Move this into Microsoft.AspNetCore.Blazor, and use DI to break the
    // coupling on Microsoft.AspNetCore.Blazor.Browser.Routing.UriHelper.
    // That's because you'd use NavLink in non-browser scenarios too (e.g., prerendering).
    // Can't do this until DI is implemented.

    public class NavLink : IComponent, IDisposable
    {
        const string CssClassAttributeName = "class";
        const string HrefAttributeName = "href";

        private RenderHandle _renderHandle;
        private bool _isActive;

        private RenderFragment _childContent;
        private string _cssClass;
        private string _href;
        private string _hrefAbsolute;
        private IDictionary<string, string> _otherAttributes;

        public void Init(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
            UriHelper.OnLocationChanged += OnLocationChanged;
        }

        public void SetParameters(ParameterCollection parameters)
        {
            _childContent = null;
            _href = null;
            _hrefAbsolute = null;
            _cssClass = null;
            _otherAttributes?.Clear();
            foreach (var kvp in parameters)
            {
                switch (kvp.Name)
                {
                    case RenderTreeBuilder.ChildContent:
                        _childContent = kvp.Value as RenderFragment;
                        break;
                    case CssClassAttributeName:
                        _cssClass = kvp.Value as string;
                        break;
                    case HrefAttributeName:
                        _href = kvp.Value as string;
                        _hrefAbsolute = UriHelper.ToAbsoluteUri(_href).AbsoluteUri;
                        break;
                    default:
                        if (kvp.Value != null)
                        {
                            if (_otherAttributes == null)
                            {
                                _otherAttributes = new Dictionary<string, string>();
                            }
                            _otherAttributes.Add(kvp.Name, kvp.Value.ToString());
                        }
                        break;
                }
            }

            _isActive = UriHelper.GetAbsoluteUri().Equals(_hrefAbsolute, StringComparison.Ordinal);
            _renderHandle.Render(Render);
        }

        public void Dispose()
        {
            UriHelper.OnLocationChanged -= OnLocationChanged;
        }

        private void OnLocationChanged(object sender, string e)
        {
            var shouldBeActiveNow = UriHelper.GetAbsoluteUri().Equals(
                _hrefAbsolute,
                StringComparison.Ordinal);

            if (shouldBeActiveNow != _isActive)
            {
                _isActive = shouldBeActiveNow;

                if (_renderHandle.IsInitialized)
                {
                    _renderHandle.Render(Render);
                }
            }
        }

        private void Render(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "a");

            if (!string.IsNullOrEmpty(_href))
            {
                builder.AddAttribute(0, HrefAttributeName, _href);
            }

            var combinedClassValue = CombineWithSpace(_cssClass, _isActive ? "active" : null);
            if (combinedClassValue != null)
            {
                builder.AddAttribute(0, CssClassAttributeName, combinedClassValue);
            }

            if (_otherAttributes != null)
            {
                foreach (var kvp in _otherAttributes)
                {
                    builder.AddAttribute(0, kvp.Key, kvp.Value);
                }
            }

            if (_childContent != null)
            {
                builder.AddContent(1, _childContent);
            }

            builder.CloseElement();
        }

        private string CombineWithSpace(string str1, string str2)
            => str1 == null ? str2
            : (str2 == null ? str1 : $"{str1} {str2}");
    }
}
