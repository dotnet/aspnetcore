// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal partial class HttpResponseTrailers : HttpHeaders
{
    public Func<string, Encoding?> EncodingSelector { get; set; }

    public HttpResponseTrailers(Func<string, Encoding?>? encodingSelector = null)
    {
        EncodingSelector = encodingSelector ?? KestrelServerOptions.DefaultHeaderEncodingSelector;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
    {
        return GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SetValueUnknown(string key, StringValues value)
    {
        ValidateHeaderNameCharacters(key);
        Unknown[GetInternedHeaderName(key)] = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool AddValueUnknown(string key, StringValues value)
    {
        ValidateHeaderNameCharacters(key);
        Unknown.Add(GetInternedHeaderName(key), value);
        // Return true, above will throw and exit for false
        return true;
    }

    public override StringValues HeaderConnection { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public partial struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
    {
        private readonly HttpResponseTrailers _collection;
        private long _currentBits;
        private int _next;
        private KeyValuePair<string, StringValues> _current;
        private KnownHeaderType _currentKnownType;
        private readonly bool _hasUnknown;
        private Dictionary<string, StringValues>.Enumerator _unknownEnumerator;

        internal Enumerator(HttpResponseTrailers collection)
        {
            _collection = collection;
            _currentBits = collection._bits;
            _next = _currentBits != 0 ? BitOperations.TrailingZeroCount(_currentBits) : -1;
            _current = default;
            _currentKnownType = default;
            _hasUnknown = collection.MaybeUnknown != null;
            _unknownEnumerator = _hasUnknown
                ? collection.MaybeUnknown!.GetEnumerator()
                : default;
        }

        public readonly KeyValuePair<string, StringValues> Current => _current;

        internal readonly KnownHeaderType CurrentKnownType => _currentKnownType;

        readonly object IEnumerator.Current => _current;

        public readonly void Dispose()
        {
        }

        public void Reset()
        {
            _currentBits = _collection._bits;
            _next = _currentBits != 0 ? BitOperations.TrailingZeroCount(_currentBits) : -1;
        }
    }
}
