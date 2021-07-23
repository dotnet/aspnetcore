// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Ignitor
{
#nullable enable
    public class ComponentState
    {
        public ComponentState(int componentId)
        {
            ComponentId = componentId;
        }

        public int ComponentId { get; }
        public IComponent? Component { get; }
    }
#nullable restore
}
