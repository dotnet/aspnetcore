// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class ServerAddressesFeature : IServerAddressesFeature
    {
        public ICollection<string> Addresses { get; set; } = Array.Empty<string>();
        public bool PreferHostingUrls { get; set; }
    }
}
