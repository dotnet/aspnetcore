// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class HtmlPageContext
    {
        private readonly IDictionary<string, object> _properties;

        protected HtmlPageContext() : this(new Dictionary<string, object>())
        {
        }

        protected HtmlPageContext(HtmlPageContext currentContext)
            : this(new Dictionary<string, object>(currentContext._properties))
        {
        }

        private HtmlPageContext(IDictionary<string, object> properties)
        {
            _properties = properties;
        }

        protected TValue GetValue<TValue>(string key) =>
            _properties.TryGetValue(key, out var rawValue) ? (TValue)rawValue : default;

        protected void SetValue(string key, object value) =>
            _properties[key] = value;
    }
}