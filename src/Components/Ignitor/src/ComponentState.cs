// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
