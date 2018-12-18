// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNetCore.Components.Browser
{
    // Provides mechanisms for locating <see cref="Renderer"/> instances
    // by ID. This is used when receiving incoming events. It also implicitly
    // roots <see cref="Renderer"/> instances and their associated component instances
    // so they cannot be GCed while they are still registered for events.

    /// <summary>
    /// Framework infrastructure, not intended to be used by application code.
    /// </summary>
    internal class RendererRegistry
    {
        private static AsyncLocal<RendererRegistry> _current;
        private static readonly RendererRegistry _globalRegistry;

        // By default the registry will be set to a default value. This means that
        // things will 'just work when running in the browser.
        //
        // Running in Server-Side Blazor - any call into the Circuit will set this value via
        // the async local. This will ensure that the incoming call can resolve the correct
        // renderer associated with the user context.
        static RendererRegistry()
        {
            _current = new AsyncLocal<RendererRegistry>();
            _globalRegistry = new RendererRegistry(); 
        }

        /// <summary>
        /// Framework infrastructure, not intended to be used by application code.
        /// </summary>
        public static RendererRegistry Current => _current.Value ?? _globalRegistry;

        /// <summary>
        /// Framework infrastructure, not intended by used by application code.
        /// </summary>
        public static void SetCurrentRendererRegistry(RendererRegistry registry)
        {
            _current.Value = registry;
        }

        private int _nextId;
        private IDictionary<int, Renderer> _renderers = new Dictionary<int, Renderer>();


        /// <summary>
        /// Framework infrastructure, not intended by used by application code.
        /// </summary>
        public int Add(Renderer renderer)
        {
            lock (_renderers)
            {
                var id = _nextId++;
                _renderers.Add(id, renderer);
                return id;
            }
        }

        /// <summary>
        /// Framework infrastructure, not intended by used by application code.
        /// </summary>
        public Renderer Find(int rendererId)
        {
            lock (_renderers)
            {
                return _renderers[rendererId];
            }
        }

        /// <summary>
        /// Framework infrastructure, not intended by used by application code.
        /// </summary>
        public bool TryRemove(int rendererId)
        {
            lock (_renderers)
            {
                if (_renderers.ContainsKey(rendererId))
                {
                    _renderers.Remove(rendererId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
