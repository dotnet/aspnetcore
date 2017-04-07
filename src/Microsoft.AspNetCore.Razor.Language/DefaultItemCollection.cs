// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultItemCollection : ItemCollection
    {
        private readonly Dictionary<object, object> _items;

        public DefaultItemCollection()
        {
            _items = new Dictionary<object, object>();
        }

        public override object this[object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                object value;
                _items.TryGetValue(key, out value);
                return value;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                _items[key] = value;
            }
        }
    }
}
