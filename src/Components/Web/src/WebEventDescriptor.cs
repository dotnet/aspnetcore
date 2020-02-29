// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
    /// of the Blazor framework. These types will change in a future release.
    /// </summary>
    public sealed class WebEventDescriptor
    {
        // We split the incoming event data in two, because we don't know what type
        // to use when deserializing the args until we've deserialized the descriptor.
        // This class represents the first half of the parsing process.
        // It's public only because it's part of the signature of a [JSInvokable] method.

        /// <summary>
        /// For framework use only.
        /// </summary>
        public int BrowserRendererId { get; set; }

        /// <summary>
        /// For framework use only.
        /// </summary>
        public ulong EventHandlerId { get; set; }

        /// <summary>
        /// For framework use only.
        /// </summary>
        public string EventArgsType { get; set; }

        /// <summary>
        /// For framework use only.
        /// </summary>
        public EventFieldInfo EventFieldInfo { get; set; }
    }
}
