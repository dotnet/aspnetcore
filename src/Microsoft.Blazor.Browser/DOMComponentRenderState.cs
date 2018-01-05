// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.UITree;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Blazor.Browser
{
    /// <summary>
    /// Tracks the rendering state associated with an <see cref="IComponent"/> that is
    /// being displayed in the DOM.
    /// </summary>
    internal class DOMComponentRenderState
    {
        private static ConditionalWeakTable<IComponent, DOMComponentRenderState> _stateInstances
            = new ConditionalWeakTable<IComponent, DOMComponentRenderState>();
        private static long _nextDOMComponentId = 0;

        private readonly UITreeBuilder _uITreeBuilder; // TODO: Maintain two, so we can diff successive renders

        public string DOMComponentId { get; }

        public IComponent Component { get; }

        private DOMComponentRenderState(string componentId, IComponent component)
        {
            DOMComponentId = DOMComponentId;
            Component = component;
            _uITreeBuilder = new UITreeBuilder();
        }

        public static DOMComponentRenderState GetOrCreate(IComponent component)
        {
            lock (_stateInstances)
            {
                if (_stateInstances.TryGetValue(component, out var existingState))
                {
                    return existingState;
                }
                else
                {
                    var newId = (_nextDOMComponentId++).ToString();
                    var newState = new DOMComponentRenderState(newId, component);
                    _stateInstances.Add(component, newState);
                    return newState;
                }
            }
        }

        public ArraySegment<UITreeNode> UpdateRender()
        {
            _uITreeBuilder.Clear();
            Component.BuildUITree(_uITreeBuilder);

            // TODO: Change this to return a diff between the previous render result and this new one
            return _uITreeBuilder.GetNodes();
        }
    }
}
