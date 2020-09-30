// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Hosting.Server.Features
{
    /// <summary>
    /// Specifies the address used by the server.
    /// </summary>
    public class ServerAddressesFeature : IServerAddressesFeature
    {
        /// <inheritdoc />
        public ICollection<string> Addresses { get; } = new List<string>();

        /// <inheritdoc />
        public bool PreferHostingUrls { get; set; }
    }
}
