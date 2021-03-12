// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.HotReload
{
    /// <summary>
    /// Allows a component to receive a <see cref="HotReloadContext"/>.
    /// </summary>
    public interface IReceiveHotReloadContext : IComponent
    {
        /// <summary>
        /// Configures a component to use the hot reload context.
        /// </summary>
        /// <param name="context"></param>
        void Receive(HotReloadContext context);
    }
}
