// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Ignitor
{
    public class ComponentNode : ContainerNode
    {
        private readonly int _componentId;

        public ComponentNode(int componentId)
        {
            _componentId = componentId;
        }
    }
}
