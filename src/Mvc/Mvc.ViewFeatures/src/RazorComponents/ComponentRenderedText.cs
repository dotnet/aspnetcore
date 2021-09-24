// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Rendering
{
    internal readonly struct ComponentRenderedText
    {
        public ComponentRenderedText(int componentId, IEnumerable<string> tokens)
        {
            ComponentId = componentId;
            Tokens = tokens;
        }

        public int ComponentId { get; }

        public IEnumerable<string> Tokens { get; }
    }
}
