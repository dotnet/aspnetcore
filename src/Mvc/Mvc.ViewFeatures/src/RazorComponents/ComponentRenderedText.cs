// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
