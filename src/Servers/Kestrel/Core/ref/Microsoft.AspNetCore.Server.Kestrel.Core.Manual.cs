// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal partial class KestrelServerOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>
    {
        private System.IServiceProvider _services;
        public KestrelServerOptionsSetup(System.IServiceProvider services) { }
        public void Configure(Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options) { }
    }
}
