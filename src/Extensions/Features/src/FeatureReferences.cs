// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// A reference to a collection of features.
/// </summary>
/// <typeparam name="TCache">The type of the feature.</typeparam>
public struct FeatureReferences<TCache>
{
    /// <summary>
    /// Initializes a new instance of <see cref="FeatureReferences{TCache}"/>.
    /// </summary>
    /// <param name="collection">The <see cref="IFeatureCollection"/>.</param>
    public FeatureReferences(IFeatureCollection collection)
    {
        Collection = collection;
        Cache = default;
        Revision = collection.Revision;
    }

    /// <summary>
    /// Initializes the <see cref="FeatureReferences{TCache}"/>.
    /// </summary>
    /// <param name="collection">The <see cref="IFeatureCollection"/> to initialize with.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initalize(IFeatureCollection collection)
    {
        Revision = collection.Revision;
        Collection = collection;
    }

    /// <summary>
    /// Initializes the <see cref="FeatureReferences{TCache}"/>.
    /// </summary>
    /// <param name="collection">The <see cref="IFeatureCollection"/> to initialize with.</param>
    /// <param name="revision">The version of the <see cref="IFeatureCollection"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initalize(IFeatureCollection collection, int revision)
    {
        Revision = revision;
        Collection = collection;
    }

    /// <summary>
    /// Gets the <see cref="IFeatureCollection"/>.
    /// </summary>
    public IFeatureCollection Collection { get; private set; }

    /// <summary>
    /// Gets the revision number.
    /// </summary>
    public int Revision { get; private set; }

    // cache is a public field because the code calling Fetch must
    // be able to pass ref values that "dot through" the TCache struct memory,
    // if it was a Property then that getter would return a copy of the memory
    // preventing the use of "ref"
    /// <summary>
    /// This API is part of ASP.NET Core's infrastructure and should not be referenced by application code.
    /// </summary>
    public TCache? Cache;

    // Careful with modifications to the Fetch method; it is carefully constructed for inlining
    // See: https://github.com/aspnet/HttpAbstractions/pull/704
    // This method is 59 IL bytes and at inline call depth 3 from accessing a property.
    // This combination is enough for the jit to consider it an "unprofitable inline"
    // Aggressively inlining it causes the entire call chain to dissolve:
    //
    // This means this call graph:
    //
    // HttpResponse.Headers -> Response.HttpResponseFeature -> Fetch -> Fetch      -> Revision
    //                                                               -> Collection -> Collection
    //                                                                             -> Collection.Revision
    // Has 6 calls eliminated and becomes just:                                    -> UpdateCached
    //
    // HttpResponse.Headers -> Collection.Revision
    //                      -> UpdateCached (not called on fast path)
    //
    // As this is inlined at the callsite we want to keep the method small, so it only detects
    // if a reset or update is required and all the reset and update logic is pushed to UpdateCached.
    //
    // Generally Fetch is called at a ratio > x4 of UpdateCached so this is a large gain

    /// <summary>
    /// This API is part of ASP.NET Core's infrastructure and should not be referenced by application code.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TFeature? Fetch<TFeature, TState>(
        ref TFeature? cached,
        TState state,
        Func<TState, TFeature?> factory) where TFeature : class?
    {
        var flush = false;
        var revision = Collection?.Revision ?? ContextDisposed();
        if (Revision != revision)
        {
            // Clear cached value to force call to UpdateCached
            cached = null!;
            // Collection changed, clear whole feature cache
            flush = true;
        }

        return cached ?? UpdateCached(ref cached!, state, factory, revision, flush);
    }

    // Update and cache clearing logic, when the fast-path in Fetch isn't applicable
    private TFeature? UpdateCached<TFeature, TState>(ref TFeature? cached, TState state, Func<TState, TFeature?> factory, int revision, bool flush) where TFeature : class?
    {
        if (flush)
        {
            // Collection detected as changed, clear cache
            Cache = default;
        }

        cached = Collection.Get<TFeature>();
        if (cached == null)
        {
            // Item not in collection, create it with factory
            cached = factory(state);
            // Add item to IFeatureCollection
            Collection.Set(cached);
            // Revision changed by .Set, update revision to new value
            Revision = Collection.Revision;
        }
        else if (flush)
        {
            // Cache was cleared, but item retrieved from current Collection for version
            // so use passed in revision rather than making another virtual call
            Revision = revision;
        }

        return cached;
    }

    /// <summary>
    /// This API is part of ASP.NET Core's infrastructure and should not be referenced by application code.
    /// </summary>
    public TFeature? Fetch<TFeature>(ref TFeature? cached, Func<IFeatureCollection, TFeature?> factory)
        where TFeature : class? => Fetch(ref cached, Collection, factory);

    private static int ContextDisposed()
    {
        ThrowContextDisposed();
        return 0;
    }

    private static void ThrowContextDisposed()
    {
        throw new ObjectDisposedException(nameof(Collection), nameof(IFeatureCollection) + " has been disposed.");
    }
}
