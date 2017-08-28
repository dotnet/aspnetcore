// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Context containing the common services on the <see cref="IWebHost" />. Some properties may be null until set by the <see cref="IWebHost" />.
    /// </summary>
    public class WebHostBuilderContext
    {
        /// <summary>
        /// The <see cref="IHostingEnvironment" /> initialized by the <see cref="IWebHost" />.
        /// </summary>
        public IHostingEnvironment HostingEnvironment { get; set; }

        /// <summary>
        /// The <see cref="IConfiguration" /> containing the merged configuration of the application and the <see cref="IWebHost" />.
        /// </summary>
        public IConfiguration Configuration { get; set; }
    }
}
