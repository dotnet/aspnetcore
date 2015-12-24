// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public partial class FrameRequestHeaders : FrameHeaders
    {
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
        {
            return GetEnumerator();
        }

        public partial struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
        {
            private readonly FrameRequestHeaders _collection;
            private readonly long _bits;
            private int _state;
            private KeyValuePair<string, StringValues> _current;
            private readonly bool _hasUnknown;
            private Dictionary<string, StringValues>.Enumerator _unknownEnumerator;

            internal Enumerator(FrameRequestHeaders collection)
            {
                _collection = collection;
                _bits = collection._bits;
                _state = 0;
                _current = default(KeyValuePair<string, StringValues>);
                _hasUnknown = collection.MaybeUnknown != null;
                _unknownEnumerator = _hasUnknown
                    ? collection.MaybeUnknown.GetEnumerator()
                    : default(Dictionary<string, StringValues>.Enumerator);
            }

            public KeyValuePair<string, StringValues> Current => _current;

            object IEnumerator.Current => _current;

            public void Dispose()
            {
            }

            public void Reset()
            {
                _state = 0;
            }
        }
    }
}
