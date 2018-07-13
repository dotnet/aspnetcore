// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public partial class HttpResponseHeaders : HttpHeaders
    {
        private static readonly byte[] _CrLf = new[] { (byte)'\r', (byte)'\n' };
        private static readonly byte[] _colonSpace = new[] { (byte)':', (byte)' ' };

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
        {
            return GetEnumerator();
        }

        internal void CopyTo(ref CountingBufferWriter<PipeWriter> buffer)
        {
            CopyToFast(ref buffer);
            if (MaybeUnknown != null)
            {
                foreach (var kv in MaybeUnknown)
                {
                    foreach (var value in kv.Value)
                    {
                        if (value != null)
                        {
                            buffer.Write(_CrLf);
                            PipelineExtensions.WriteAsciiNoValidation(ref buffer, kv.Key);
                            buffer.Write(_colonSpace);
                            PipelineExtensions.WriteAsciiNoValidation(ref buffer, value);
                        }
                    }
                }
            }
        }

        private static long ParseContentLength(string value)
        {
            long parsed;
            if (!HeaderUtilities.TryParseNonNegativeInt64(value, out parsed))
            {
                ThrowInvalidContentLengthException(value);
            }

            return parsed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetValueUnknown(string key, in StringValues value)
        {
            ValidateHeaderNameCharacters(key);
            Unknown[key] = value;
        }

        private static void ThrowInvalidContentLengthException(string value)
        {
            throw new InvalidOperationException(CoreStrings.FormatInvalidContentLength_InvalidNumber(value));
        }

        public partial struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
        {
            private readonly HttpResponseHeaders _collection;
            private readonly long _bits;
            private int _state;
            private KeyValuePair<string, StringValues> _current;
            private readonly bool _hasUnknown;
            private Dictionary<string, StringValues>.Enumerator _unknownEnumerator;

            internal Enumerator(HttpResponseHeaders collection)
            {
                _collection = collection;
                _bits = collection._bits;
                _state = 0;
                _current = default;
                _hasUnknown = collection.MaybeUnknown != null;
                _unknownEnumerator = _hasUnknown
                    ? collection.MaybeUnknown.GetEnumerator()
                    : default;
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
