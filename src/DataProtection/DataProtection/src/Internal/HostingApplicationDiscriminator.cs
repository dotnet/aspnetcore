// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    internal class HostingApplicationDiscriminator : IApplicationDiscriminator
    {
        private readonly IHostEnvironment? _hosting;

        // the optional constructor for when IHostingEnvironment is not available from DI
        public HostingApplicationDiscriminator()
        {
        }

        public HostingApplicationDiscriminator(IHostEnvironment hosting)
        {
            _hosting = hosting;
        }

        public string? Discriminator => _hosting?.ContentRootPath;
    }
}
