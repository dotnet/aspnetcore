// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// A special subclass of <see cref="ArrayBuilder{T}"/> that contains methods optimized for appending <see cref="RenderTreeFrame"/> entries.
    /// </summary>
    internal class RenderTreeArrayBuilder : ArrayBuilder<RenderTreeFrame>
    {
    }
}
