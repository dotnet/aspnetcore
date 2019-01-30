// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
