// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Ignitor
{
    public readonly struct CapturedAttachComponentCall
    {
        public CapturedAttachComponentCall(int componentId, string selector)
        {
            ComponentId = componentId;
            Selector = selector;
        }

        public int ComponentId { get; }
        public string Selector { get; }
    }
}
