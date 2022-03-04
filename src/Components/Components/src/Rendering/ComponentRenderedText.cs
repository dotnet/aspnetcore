// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Represents the result of rendering a component into static html.
    /// </summary>
    public readonly struct ComponentRenderedText
    {
        internal ComponentRenderedText(int componentId, IEnumerable<string> tokens)
        {
            ComponentId = componentId;
            Tokens = tokens;
        }

        /// <summary>
        /// Gets the id associated with the component.
        /// </summary>
        public int ComponentId { get; }

        /// <summary>
        /// Gets the sequence of tokens that when concatenated represent the html for the rendered component.
        /// </summary>
        public IEnumerable<string> Tokens { get; }
    }
}
