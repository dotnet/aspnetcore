// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser.Interop;
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
        // Track the associations between component IDs, IComponent instances, and
        // DOMComponentRenderState instances, but without pinning any IComponent instances
        // in memory.
        // TODO: Instead of storing these as statics, have some kind of RenderContext instance
        // that holds them. It can also hold a reference to the root component, since otherwise
        // there isn't anything stopping the whole hierarchy of components from being GCed.
        private static ConditionalWeakTable<IComponent, DOMComponentRenderState> _renderStatesByComponent
            = new ConditionalWeakTable<IComponent, DOMComponentRenderState>();
        private static WeakValueDictionary<string, DOMComponentRenderState> _renderStatesByComponentId
            = new WeakValueDictionary<string, DOMComponentRenderState>();
        private static long _nextDOMComponentId = 0;

        private readonly UITreeBuilder _uITreeBuilder; // TODO: Maintain two, so we can diff successive renders

        public string DOMComponentId { get; }

        public IComponent Component { get; }

        private DOMComponentRenderState(string componentId, IComponent component)
        {
            DOMComponentId = componentId;
            Component = component;
            _uITreeBuilder = new UITreeBuilder();
        }

        public static DOMComponentRenderState GetOrCreate(IComponent component)
        {
            lock (_renderStatesByComponent)
            {
                if (_renderStatesByComponent.TryGetValue(component, out var existingState))
                {
                    return existingState;
                }
                else
                {
                    var newId = (_nextDOMComponentId++).ToString();
                    var newState = new DOMComponentRenderState(newId, component);
                    _renderStatesByComponent.Add(component, newState);
                    _renderStatesByComponentId.Add(newId, newState);
                    return newState;
                }
            }
        }

        public static DOMComponentRenderState FindByDOMComponentID(string id)
            => _renderStatesByComponentId.TryGetValue(id, out var result)
            ? result
            : throw new ArgumentException($"No component was found with ID {id}");

        private ArraySegment<UITreeNode> UpdateRender()
        {
            _uITreeBuilder.Clear();
            Component.BuildUITree(_uITreeBuilder);

            // TODO: Change this to return a diff between the previous render result and this new one
            return _uITreeBuilder.GetNodes();
        }

        public void RaiseEvent(int uiTreeNodeIndex, UIEventInfo eventInfo)
        {
            var nodes = _uITreeBuilder.GetNodes();
            var eventHandler = nodes.Array[nodes.Offset + uiTreeNodeIndex].AttributeEventHandlerValue;
            if (eventHandler == null)
            {
                throw new ArgumentException($"Cannot raise event because the specified {nameof(UITreeNode)} at index {uiTreeNodeIndex} does not have any {nameof(UITreeNode.AttributeEventHandlerValue)}.");
            }

            eventHandler.Invoke(eventInfo);
            RenderToDOM();
        }

        public void RenderToDOM()
        {
            var tree = UpdateRender();
            RegisteredFunction.InvokeUnmarshalled<string, UITreeNode[], int, object>(
                "_blazorRender",
                DOMComponentId,
                tree.Array,
                tree.Count);
        }

        public static ComponentRenderInfo GetComponentRenderInfo(string parentComponentId, string componentNodeIndexString)
        {
            var parentComponentRenderState = FindByDOMComponentID(parentComponentId);
            var parentComponentNodes = parentComponentRenderState._uITreeBuilder.GetNodes();
            var component = parentComponentNodes
                .Array[int.Parse(componentNodeIndexString)]
                .Component;
            if (component == null )
            {
                throw new ArgumentException($"The tree entry at position {componentNodeIndexString} does not refer to a component.");
            }

            var componentRenderState = GetOrCreate(component);

            // Don't necessarily need to re-render the child at this point. Review when the
            // component lifecycle has more details (e.g., async init method)
            var componentNodes = componentRenderState.UpdateRender();
            return new ComponentRenderInfo
            {
                ComponentId = componentRenderState.DOMComponentId,
                UITree = componentNodes.Array,
                UITreeLength = componentNodes.Count
            };
        }

        public struct ComponentRenderInfo
        {
            public string ComponentId;
            public UITreeNode[] UITree;
            public int UITreeLength;
        }
    }
}
