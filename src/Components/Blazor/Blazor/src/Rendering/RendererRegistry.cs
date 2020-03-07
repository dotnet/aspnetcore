// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    internal static class RendererRegistry
    {
        // In case there are multiple concurrent Blazor renderers in the same .NET WebAssembly
        // process, we track them by ID. This allows events to be dispatched to the correct one,
        // as well as rooting them for GC purposes, since nothing would otherwise be referencing
        // them even though we might still receive incoming events from JS.

        private static int _nextId;
        private static Dictionary<int, WebAssemblyRenderer> _renderers = new Dictionary<int, WebAssemblyRenderer>();

        internal static WebAssemblyRenderer Find(int rendererId)
        {
            return _renderers.ContainsKey(rendererId)
                ? _renderers[rendererId]
                : throw new ArgumentException($"There is no renderer with ID {rendererId}.");
        }

        public static int Add(WebAssemblyRenderer renderer)
        {
            var id = _nextId++;
            _renderers.Add(id, renderer);
            return id;
        }

        public static bool TryRemove(int rendererId)
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
