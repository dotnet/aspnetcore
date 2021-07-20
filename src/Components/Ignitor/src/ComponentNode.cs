// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Ignitor
{
    public class ComponentNode : ContainerNode
    {
        private readonly int _componentId;

        public ComponentNode(int componentId)
        {
            _componentId = componentId;
        }

        public int ComponentId => _componentId;
    }
}
