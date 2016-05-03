// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Features
{
    public struct FeatureReferences<TCache>
    {
        public FeatureReferences(IFeatureCollection collection)
        {
            Collection = collection;
            Cache = default(TCache);
            Revision = collection.Revision;
        }

        public IFeatureCollection Collection { get; private set; }
        public int Revision { get; private set; }

        // cache is a public field because the code calling Fetch must
        // be able to pass ref values that "dot through" the TCache struct memory, 
        // if it was a Property then that getter would return a copy of the memory
        // preventing the use of "ref"
        public TCache Cache; 

        public TFeature Fetch<TFeature, TState>(
            ref TFeature cached,
            TState state,
            Func<TState, TFeature> factory) where TFeature : class
        {
            var revision = Collection.Revision;
            if (Revision == revision)
            {
                // collection unchanged, use cached
                return cached ?? UpdateCached(ref cached, state, factory);
            }

            // collection changed, clear cache
            Cache = default(TCache);
            // empty cache is current revision
            Revision = revision;

            return UpdateCached(ref cached, state, factory);
        }

        private TFeature UpdateCached<TFeature, TState>(ref TFeature cached, TState state, Func<TState, TFeature> factory) where TFeature : class
        {
            cached = Collection.Get<TFeature>();
            if (cached == null)
            {
                // create if item not in collection
                cached = factory(state);
                Collection.Set(cached);
                // Revision changed by .Set, update revision
                Revision = Collection.Revision;
            }

            return cached;
        }

        public TFeature Fetch<TFeature>(ref TFeature cached, Func<IFeatureCollection, TFeature> factory)
            where TFeature : class => Fetch(ref cached, Collection, factory);
    }
}