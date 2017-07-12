// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public partial class FrameRequestHeaders : FrameHeaders
    {
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
            Unknown[key] = value;
        }

        public unsafe void Append(Span<byte> name, string value)
        {
            fixed (byte* namePtr = &name.DangerousGetPinnableReference())
            {
                Append(namePtr, name.Length, value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void AppendUnknownHeaders(byte* pKeyBytes, int keyLength, string value)
        {
            string key = new string('\0', keyLength);
            fixed (char* keyBuffer = key)
            {
                if (!StringUtilities.TryGetAsciiString(pKeyBytes, keyBuffer, keyLength))
                {
                    throw BadHttpRequestException.GetException(RequestRejectionReason.InvalidCharactersInHeaderName);
                }
            }

            StringValues existing;
            Unknown.TryGetValue(key, out existing);
            Unknown[key] = AppendValue(existing, value);
        }

        private static void ThrowInvalidContentLengthException(string value)
        {
            throw BadHttpRequestException.GetException(RequestRejectionReason.InvalidContentLength, value);
        }

        private static void ThrowMultipleContentLengthsException()
        {
            throw BadHttpRequestException.GetException(RequestRejectionReason.MultipleContentLengths);
        }

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
