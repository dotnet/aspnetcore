// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for selecting default file names.
    /// </summary>
    public class DefaultFilesOptions : SharedOptionsBase
    {
        /// <summary>
        /// Configuration for the DefaultFilesMiddleware.
        /// </summary>
        public DefaultFilesOptions()
            : this(new SharedOptions())
        {
        }

        /// <summary>
        /// Configuration for the DefaultFilesMiddleware.
        /// </summary>
        /// <param name="sharedOptions"></param>
        public DefaultFilesOptions(SharedOptions sharedOptions)
            : base(sharedOptions)
        {
            // Prioritized list
            DefaultFileNames = new List<string>
            {
                "default.htm",
                "default.html",
                "index.htm",
                "index.html",
            };
        }

        /// <summary>
        /// An ordered list of file names to select by default. List length and ordering may affect performance.
        /// </summary>
        public IList<string> DefaultFileNames { get; set; }
    }
}
