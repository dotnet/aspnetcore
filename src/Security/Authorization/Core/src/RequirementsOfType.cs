// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Equivalent to <c>requirements.OfType&lt;T&gt;()</c>, but indexes into the <see cref="ReadOnlyCollection{T}"/>
/// returned by <see cref="AuthorizationPolicy.Requirements"/> to avoid allocating an iterator.
/// </summary>
internal readonly struct RequirementsOfType<T>
{
    private readonly IEnumerable<IAuthorizationRequirement> _requirements;

    public RequirementsOfType(IEnumerable<IAuthorizationRequirement> requirements)
    {
        _requirements = requirements;
    }

    public Enumerator GetEnumerator() => new(_requirements);

    public struct Enumerator : IDisposable
    {
        private readonly ReadOnlyCollection<IAuthorizationRequirement>? _list;
        private readonly IEnumerator<T>? _fallback;
        private int _index;
        private T _current;

        internal Enumerator(IEnumerable<IAuthorizationRequirement> requirements)
        {
            _list = requirements as ReadOnlyCollection<IAuthorizationRequirement>;
            _fallback = _list is null ? requirements.OfType<T>().GetEnumerator() : null;
            _index = -1;
            _current = default!;
        }

        public readonly T Current => _list is null ? _fallback!.Current : _current;

        public bool MoveNext()
        {
            if (_list is null)
            {
                return _fallback!.MoveNext();
            }

            while (++_index < _list.Count)
            {
                if (_list[_index] is T requirement)
                {
                    _current = requirement;
                    return true;
                }
            }

            return false;
        }

        public readonly void Dispose() => _fallback?.Dispose();
    }
}
