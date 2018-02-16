// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class ServerAddressesFeature : IServerAddressesFeature
    {
        public ICollection<string> Addresses { get; } = new List<string>();
        public bool PreferHostingUrls { get; set; }
    }
}
