// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class HtmlPageContext
    {
        private readonly IDictionary<string, string> _properties = 
            new Dictionary<string, string>();

        public string this[string key]
        {
            get => _properties[key];
            set => _properties[key] = value;
        }
    }
}