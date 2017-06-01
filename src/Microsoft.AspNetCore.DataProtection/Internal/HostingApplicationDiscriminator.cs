// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    internal class HostingApplicationDiscriminator : IApplicationDiscriminator
    {
        private readonly IHostingEnvironment _hosting;

        // the optional constructor for when IHostingEnvironment is not available from DI
        public HostingApplicationDiscriminator()
        {
        }

        public HostingApplicationDiscriminator(IHostingEnvironment hosting)
        {
            _hosting = hosting;
        }

        public string Discriminator => _hosting?.ContentRootPath;
    }
}
