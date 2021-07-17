// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Allows a component to interact with its renderer.
    /// </summary>
    public readonly struct RenderHandle
    {
        private readonly Renderer? _renderer;
        private readonly int _componentId;

        internal RenderHandle(Renderer renderer, int componentId)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _componentId = componentId;
        }

        /// <summary>
        /// Gets the <see cref="Components.Dispatcher" /> associated with the component.
        /// </summary>
        public Dispatcher Dispatcher
        {
            get
            {
                if (_renderer == null)
                {
                    ThrowNotInitialized();
                }

                return _renderer.Dispatcher;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="RenderHandle"/> has been
        /// initialized and is ready to use.
        /// </summary>
        public bool IsInitialized => _renderer is not null;

        /// <summary>
        /// Gets a value that determines if the <see cref="Renderer"/> is triggering a render in response to a metadata update (hot-reload) change.
        /// </summary>
        public bool IsRenderingOnMetadataUpdate => TestableMetadataUpdate.IsSupported && (_renderer?.IsRenderingOnMetadataUpdate ?? false);

        internal bool IsRendererDisposed => _renderer?.Disposed
            ?? throw new InvalidOperationException("No renderer has been initialized.");

        /// <summary>
        /// Notifies the renderer that the component should be rendered.
        /// </summary>
        /// <param name="renderFragment">The content that should be rendered.</param>
        public void Render(RenderFragment renderFragment)
        {
            if (_renderer == null)
            {
                ThrowNotInitialized();
            }

            _renderer.AddToRenderQueue(_componentId, renderFragment);
        }

        [DoesNotReturn]
        private static void ThrowNotInitialized()
        {
            throw new InvalidOperationException("The render handle is not yet assigned.");
        }
    }
}
