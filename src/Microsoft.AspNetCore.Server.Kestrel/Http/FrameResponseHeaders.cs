// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public partial class FrameResponseHeaders : FrameHeaders
    {
        private static readonly byte[] _CrLf = new[] { (byte)'\r', (byte)'\n' };
        private static readonly byte[] _colonSpace = new[] { (byte)':', (byte)' ' };

        public bool HasConnection => HeaderConnection.Count != 0;

        public bool HasTransferEncoding => HeaderTransferEncoding.Count != 0;

        public bool HasContentLength => HeaderContentLength.Count != 0;

        public bool HasServer => HeaderServer.Count != 0;

        public bool HasDate => HeaderDate.Count != 0;

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
        {
            return GetEnumerator();
        }

        public void CopyTo(ref MemoryPoolIterator output)
        {
            CopyToFast(ref output);
            if (MaybeUnknown != null)
            {
                foreach (var kv in MaybeUnknown)
                {
                    foreach (var value in kv.Value)
                    {
                        if (value != null)
                        {
                            output.CopyFrom(_CrLf, 0, 2);
                            output.CopyFromAscii(kv.Key);
                            output.CopyFrom(_colonSpace, 0, 2);
                            output.CopyFromAscii(value);
                        }
                    }
                }
            }
        }

        public partial struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
        {
            private readonly FrameResponseHeaders _collection;
            private readonly long _bits;
            private int _state;
            private KeyValuePair<string, StringValues> _current;
            private readonly bool _hasUnknown;
            private Dictionary<string, StringValues>.Enumerator _unknownEnumerator;

            internal Enumerator(FrameResponseHeaders collection)
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
