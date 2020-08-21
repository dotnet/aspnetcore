// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
