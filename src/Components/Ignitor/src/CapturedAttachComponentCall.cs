// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
