// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal class IISRewriteMap
    {
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>();

        public IISRewriteMap(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            Name = name;
        }

        public string Name { get; }

        public string? this[string key]
        {
            get
            {
                return _map.TryGetValue(key, out var value) ? value : null;
            }
            set
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _map[key] = value;
            }
        }
    }
}
