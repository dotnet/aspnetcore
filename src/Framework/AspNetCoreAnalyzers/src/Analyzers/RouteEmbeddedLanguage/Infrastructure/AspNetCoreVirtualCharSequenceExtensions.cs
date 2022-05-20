// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class AspNetCoreVirtualCharSequenceExtensions
{
    public static AspNetCoreVirtualChar First(this AspNetCoreVirtualCharSequence virtualChars)
    {
        return virtualChars[0];
    }

    public static AspNetCoreVirtualChar Last(this AspNetCoreVirtualCharSequence virtualChars)
    {
        return virtualChars[virtualChars.Length - 1];
    }

    public static bool Contains(this AspNetCoreVirtualCharSequence virtualChars, AspNetCoreVirtualChar virtualChar) => IndexOf(virtualChars, virtualChar) >= 0;

    public static int IndexOf(this AspNetCoreVirtualCharSequence virtualChars, AspNetCoreVirtualChar virtualChar) 
    {
        var index = 0;
        foreach (var ch in virtualChars)
        {
            if (ch.Equals(virtualChar))
            {
                return index;
            }
            index++;
        }

        return -1;
    }

    public static Enumerator GetEnumerator(this AspNetCoreVirtualCharSequence virtualChars) => new(virtualChars);

    public struct Enumerator : IEnumerator<AspNetCoreVirtualChar>
    {
        private readonly AspNetCoreVirtualCharSequence _virtualCharSequence;
        private int _position;

        public Enumerator(AspNetCoreVirtualCharSequence virtualCharSequence)
        {
            _virtualCharSequence = virtualCharSequence;
            _position = -1;
        }

        public bool MoveNext() => ++_position < _virtualCharSequence.Length;
        public AspNetCoreVirtualChar Current => _virtualCharSequence[_position];

        void IEnumerator.Reset()
            => _position = -1;

        object? IEnumerator.Current => this.Current;
        void IDisposable.Dispose() { }
    }

    public static bool IsDefault(this AspNetCoreVirtualCharSequence virtualChars)
        => virtualChars.Equals(default(AspNetCoreVirtualCharSequence));
}
