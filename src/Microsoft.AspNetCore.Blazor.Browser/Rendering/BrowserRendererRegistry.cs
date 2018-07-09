// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Browser.Rendering
{
    /// <summary>
    /// Provides mechanisms for locating <see cref="BrowserRenderer"/> instances
    /// by ID. This is used when receiving incoming events from the browser. It
    /// implicitly ensures that the <see cref="BrowserRenderer"/> instances and
    /// their associated component instances aren't GCed when events may still
    /// be received for them.
    /// </summary>
    internal class BrowserRendererRegistry
    {
        private int _nextId;
        private IDictionary<int, BrowserRenderer> _browserRenderers
            = new Dictionary<int, BrowserRenderer>();

        /// TODO: Don't have just one global static instance.
        /// For multi-user scenarios, we need the current instance to be distinct for each user,
        /// and ensure it's always set to that user's value whenever there are incoming JS interop
        /// calls. We could store the value in the IServiceProvider but then we still need some way
        /// of reaching the current user's IServiceProvider inside calls to .NET from JS interop.
        internal static BrowserRendererRegistry CurrentUserInstance { get; }
            = new BrowserRendererRegistry();

        /// <summary>
        /// Adds the <paramref name="browserRenderer"/> and gets a unique identifier for it.
        /// </summary>
        /// <param name="browserRenderer"></param>
        /// <returns>A unique identifier for the <paramref name="browserRenderer"/>.</returns>
        public int Add(BrowserRenderer browserRenderer)
        {
            lock (_browserRenderers)
            {
                var id = _nextId++;
                _browserRenderers.Add(id, browserRenderer);
                return id;
            }
        }

        /// <summary>
        /// Gets the <see cref="BrowserRenderer"/> with the specified
        /// <paramref name="browserRendererId"/>.
        /// </summary>
        /// <param name="browserRendererId">The identifier of the instance to be returned.</param>
        /// <returns>The corresponding <see cref="BrowserRenderer"/> instance.</returns>
        public BrowserRenderer Find(int browserRendererId)
        {
            lock (_browserRenderers)
            {
                return _browserRenderers[browserRendererId];
            }
        }

        /// <summary>
        /// Removes the <see cref="BrowserRenderer"/> with the specified identifier, if present.
        /// </summary>
        /// <param name="browserRendererId">The identifier of the <see cref="BrowserRenderer"/> to remove.</param>
        /// <returns><see langword="true"/> if the <see cref="BrowserRenderer"/> was present; otherwise <see langword="false" />.</returns>
        public bool TryRemove(int browserRendererId)
        {
            lock (_browserRenderers)
            {
                if (_browserRenderers.ContainsKey(browserRendererId))
                {
                    _browserRenderers.Remove(browserRendererId);
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
