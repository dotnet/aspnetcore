// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Dispatcher;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteValueDictionary : DispatcherValueCollection
    {
        public RouteValueDictionary()
            : base()
        {
        }

        public RouteValueDictionary(object obj)
            : base(obj)
        {
        }

        // Required to avoid a breaking change in the split of RVD/DVC
        public new Enumerator GetEnumerator() => new Enumerator(this);

        // Required to avoid a breaking change in the split of RVD/DVC
        public new struct Enumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private DispatcherValueCollection.Enumerator _inner;

            public Enumerator(RouteValueDictionary dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }

                _inner = ((DispatcherValueCollection)dictionary).GetEnumerator();
            }

            public KeyValuePair<string, object> Current => _inner.Current;

            object IEnumerator.Current => _inner.Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return _inner.MoveNext();
            }

            public void Reset()
            {
                _inner.Reset();
            }
        }
    }
}
