// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Information about the current routing path.
/// </summary>
[DebuggerDisplay("Count = {Values.Count}")]
[DebuggerTypeProxy(typeof(RouteDataDebugView))]
public class RouteData
{
    private RouteValueDictionary? _dataTokens;
    private List<IRouter>? _routers;
    private RouteValueDictionary? _values;

    /// <summary>
    /// Creates a new instance of <see cref="RouteData"/> instance.
    /// </summary>
    public RouteData()
    {
        // Perf: Avoid allocating collections unless needed.
    }

    /// <summary>
    /// Creates a new instance of <see cref="RouteData"/> instance with values copied from <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The other <see cref="RouteData"/> instance to copy.</param>
    public RouteData(RouteData other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // Perf: Avoid allocating collections unless we need to make a copy.
        if (other._routers != null)
        {
            _routers = new List<IRouter>(other.Routers);
        }

        if (other._dataTokens != null)
        {
            _dataTokens = new RouteValueDictionary(other._dataTokens);
        }

        if (other._values != null)
        {
            _values = new RouteValueDictionary(other._values);
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="RouteData"/> instance with the specified values.
    /// </summary>
    /// <param name="values">The <see cref="RouteValueDictionary"/> values.</param>
    public RouteData(RouteValueDictionary values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _values = values;
    }

    /// <summary>
    /// Gets the data tokens produced by routes on the current routing path.
    /// </summary>
    public RouteValueDictionary DataTokens
    {
        get
        {
            if (_dataTokens == null)
            {
                _dataTokens = new RouteValueDictionary();
            }

            return _dataTokens;
        }
    }

    /// <summary>
    /// Gets the list of <see cref="IRouter"/> instances on the current routing path.
    /// </summary>
    public IList<IRouter> Routers
    {
        get
        {
            if (_routers == null)
            {
                _routers = new List<IRouter>();
            }

            return _routers;
        }
    }

    /// <summary>
    /// Gets the values produced by routes on the current routing path.
    /// </summary>
    public RouteValueDictionary Values
    {
        get
        {
            if (_values == null)
            {
                _values = new RouteValueDictionary();
            }

            return _values;
        }
    }

    /// <summary>
    /// <para>
    /// Creates a snapshot of the current state of the <see cref="RouteData"/> before appending
    /// <paramref name="router"/> to <see cref="Routers"/>, merging <paramref name="values"/> into
    /// <see cref="Values"/>, and merging <paramref name="dataTokens"/> into <see cref="DataTokens"/>.
    /// </para>
    /// <para>
    /// Call <see cref="RouteDataSnapshot.Restore"/> to restore the state of this <see cref="RouteData"/>
    /// to the state at the time of calling
    /// <see cref="PushState(IRouter, RouteValueDictionary, RouteValueDictionary)"/>.
    /// </para>
    /// </summary>
    /// <param name="router">
    /// An <see cref="IRouter"/> to append to <see cref="Routers"/>. If <c>null</c>, then <see cref="Routers"/>
    /// will not be changed.
    /// </param>
    /// <param name="values">
    /// A <see cref="RouteValueDictionary"/> to merge into <see cref="Values"/>. If <c>null</c>, then
    /// <see cref="Values"/> will not be changed.
    /// </param>
    /// <param name="dataTokens">
    /// A <see cref="RouteValueDictionary"/> to merge into <see cref="DataTokens"/>. If <c>null</c>, then
    /// <see cref="DataTokens"/> will not be changed.
    /// </param>
    /// <returns>A <see cref="RouteDataSnapshot"/> that captures the current state.</returns>
    public RouteDataSnapshot PushState(IRouter? router, RouteValueDictionary? values, RouteValueDictionary? dataTokens)
    {
        // Perf: this is optimized for small list sizes, in particular to avoid overhead of a native call in
        // Array.CopyTo inside the List(IEnumerable<T>) constructor.
        List<IRouter>? routers = null;
        var count = _routers?.Count;
        if (count > 0)
        {
            Debug.Assert(_routers != null);

            routers = new List<IRouter>(count.Value);
            for (var i = 0; i < count.Value; i++)
            {
                routers.Add(_routers[i]);
            }
        }

        var snapshot = new RouteDataSnapshot(
            this,
            _dataTokens?.Count > 0 ? new RouteValueDictionary(_dataTokens) : null,
            routers,
            _values?.Count > 0 ? new RouteValueDictionary(_values) : null);

        if (router != null)
        {
            Routers.Add(router);
        }

        if (values != null)
        {
            foreach (var kvp in values)
            {
                if (kvp.Value != null)
                {
                    Values[kvp.Key] = kvp.Value;
                }
            }
        }

        if (dataTokens != null)
        {
            foreach (var kvp in dataTokens)
            {
                DataTokens[kvp.Key] = kvp.Value;
            }
        }

        return snapshot;
    }

    private sealed class RouteDataDebugView(RouteData routeData)
    {
        private readonly RouteData _routeData = routeData;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DictionaryItemDebugView<string, object?>[] Items => _routeData.Values.Select(pair => new DictionaryItemDebugView<string, object>(pair)).ToArray();
    }

    /// <summary>
    /// A snapshot of the state of a <see cref="RouteData"/> instance.
    /// </summary>
    public readonly struct RouteDataSnapshot
    {
        private readonly RouteData _routeData;
        private readonly RouteValueDictionary? _dataTokens;
        private readonly IList<IRouter>? _routers;
        private readonly RouteValueDictionary? _values;

        /// <summary>
        /// Creates a new instance of <see cref="RouteDataSnapshot"/> for <paramref name="routeData"/>.
        /// </summary>
        /// <param name="routeData">The <see cref="RouteData"/>.</param>
        /// <param name="dataTokens">The data tokens.</param>
        /// <param name="routers">The routers.</param>
        /// <param name="values">The route values.</param>
        public RouteDataSnapshot(
            RouteData routeData,
            RouteValueDictionary? dataTokens,
            IList<IRouter>? routers,
            RouteValueDictionary? values)
        {
            ArgumentNullException.ThrowIfNull(routeData);

            _routeData = routeData;
            _dataTokens = dataTokens;
            _routers = routers;
            _values = values;
        }

        /// <summary>
        /// Restores the <see cref="RouteData"/> to the captured state.
        /// </summary>
        public void Restore()
        {
            if (_routeData._dataTokens == null && _dataTokens == null)
            {
                // Do nothing
            }
            else if (_dataTokens == null)
            {
                _routeData._dataTokens!.Clear();
            }
            else
            {
                _routeData._dataTokens!.Clear();

                foreach (var kvp in _dataTokens)
                {
                    _routeData._dataTokens.Add(kvp.Key, kvp.Value);
                }
            }

            if (_routeData._routers == null && _routers == null)
            {
                // Do nothing
            }
            else if (_routers == null)
            {
                // Perf: this is optimized for small list sizes, in particular to avoid overhead of a native call in
                // Array.Clear inside the List.Clear() method.
                var routers = _routeData._routers!;
                for (var i = routers.Count - 1; i >= 0; i--)
                {
                    routers.RemoveAt(i);
                }
            }
            else
            {
                // Perf: this is optimized for small list sizes, in particular to avoid overhead of a native call in
                // Array.Clear inside the List.Clear() method.
                //
                // We want to basically copy the contents of _routers in _routeData._routers - this change does
                // that with the minimal number of reads/writes and without calling Clear().
                var routers = _routeData._routers!;
                var snapshotRouters = _routers;

                // This is made more complicated by the fact that List[int] throws if i == Count, so we have
                // to do two loops and call Add for those cases.
                var i = 0;
                for (; i < snapshotRouters.Count && i < routers.Count; i++)
                {
                    routers[i] = snapshotRouters[i];
                }

                for (; i < snapshotRouters.Count; i++)
                {
                    routers.Add(snapshotRouters[i]);
                }

                // Trim excess - again avoiding RemoveRange because it uses native methods.
                for (i = routers.Count - 1; i >= snapshotRouters.Count; i--)
                {
                    routers.RemoveAt(i);
                }
            }

            if (_routeData._values == null && _values == null)
            {
                // Do nothing
            }
            else if (_values == null)
            {
                _routeData._values!.Clear();
            }
            else
            {
                _routeData._values!.Clear();

                foreach (var kvp in _values)
                {
                    _routeData._values.Add(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
