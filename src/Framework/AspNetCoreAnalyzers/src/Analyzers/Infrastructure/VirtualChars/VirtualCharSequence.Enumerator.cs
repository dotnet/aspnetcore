// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

internal partial struct VirtualCharSequence
{
    public struct Enumerator : IEnumerator<VirtualChar>
    {
        private readonly VirtualCharSequence _virtualCharSequence;
        private int _position;

        public Enumerator(VirtualCharSequence virtualCharSequence)
        {
            _virtualCharSequence = virtualCharSequence;
            _position = -1;
        }

        public bool MoveNext() => ++_position < _virtualCharSequence.Length;
        public VirtualChar Current => _virtualCharSequence[_position];

        public void Reset()
            => _position = -1;

        object? IEnumerator.Current => this.Current;
        public void Dispose() { }
    }
}
