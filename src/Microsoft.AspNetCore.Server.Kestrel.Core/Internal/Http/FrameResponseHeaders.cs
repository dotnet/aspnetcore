// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public partial class FrameResponseHeaders : FrameHeaders
    {
        private static readonly byte[] _CrLf = new[] { (byte)'\r', (byte)'\n' };
        private static readonly byte[] _colonSpace = new[] { (byte)':', (byte)' ' };

        public bool HasConnection => HeaderConnection.Count != 0;

        public bool HasTransferEncoding => HeaderTransferEncoding.Count != 0;

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

        public void CopyTo(ref WritableBuffer output)
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
                            output.WriteFast(_CrLf);
                            output.WriteAsciiNoValidation(kv.Key);
                            output.WriteFast(_colonSpace);
                            output.WriteAsciiNoValidation(value);
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
        private void SetValueUnknown(string key, StringValues value)
        {
            ValidateHeaderCharacters(key);
            Unknown[key] = value;
        }

        private static void ThrowInvalidContentLengthException(string value)
        {
            throw new InvalidOperationException($"Invalid Content-Length: \"{value}\". Value must be a positive integral number.");
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
