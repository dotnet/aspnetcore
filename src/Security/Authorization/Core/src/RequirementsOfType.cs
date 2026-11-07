// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Equivalent to <c>requirements.OfType&lt;T&gt;()</c>, but avoids allocating on the common path.
/// </summary>
/// <remarks>
/// <para>
/// Fast path: <see cref="AuthorizationPolicy.Requirements"/> is created with <see cref="List{T}.AsReadOnly"/>, so the
/// requirements passed to <see cref="AuthorizationHandlerContext"/> are usually a <see cref="ReadOnlyCollection{T}"/>.
/// In that case the enumerator indexes into the collection directly and creates no enumerator.
/// </para>
/// <para>
/// What's saved: <c>OfType&lt;T&gt;()</c> allocates an iterator plus a boxed enumerator over the source on every
/// <see cref="IAuthorizationHandler.HandleAsync(AuthorizationHandlerContext)"/> call. This struct enumerable avoids
/// both, and is safe to use in <c>foreach</c> inside async handlers because it is not a ref struct.
/// </para>
/// <para>
/// Fallback: any other <see cref="IEnumerable{T}"/> goes through <c>OfType&lt;T&gt;()</c> unchanged.
/// </para>
/// <para>
/// <typeparamref name="T"/> is intentionally unconstrained because <see cref="Infrastructure.PassThroughAuthorizationHandler"/>
/// uses it with <see cref="IAuthorizationHandler"/>, which does not derive from <see cref="IAuthorizationRequirement"/>.
/// </para>
/// </remarks>
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
