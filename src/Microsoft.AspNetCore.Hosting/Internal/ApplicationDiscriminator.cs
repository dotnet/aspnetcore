// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.Infrastructure;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class ApplicationDiscriminator : IApplicationDiscriminator
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public ApplicationDiscriminator(IHostingEnvironment hostingEnvironment)
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            _hostingEnvironment = hostingEnvironment;
        }

        public string Discriminator => _hostingEnvironment.ContentRootPath;
    }
}