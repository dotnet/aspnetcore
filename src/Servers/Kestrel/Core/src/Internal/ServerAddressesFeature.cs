// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class ServerAddressesFeature : IServerAddressesFeature
    {
        public ServerAddressesCollection InternalCollection { get; } = new ServerAddressesCollection();

        ICollection<string> IServerAddressesFeature.Addresses => InternalCollection.PublicCollection;
        public bool PreferHostingUrls { get; set; }
    }
}
