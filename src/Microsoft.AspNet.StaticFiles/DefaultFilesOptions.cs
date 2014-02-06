// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.StaticFiles.Infrastructure;

namespace Microsoft.AspNet.StaticFiles
{
    /// <summary>
    /// Options for selecting default file names.
    /// </summary>
    public class DefaultFilesOptions : SharedOptionsBase<DefaultFilesOptions>
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
            DefaultFileNames = new List<string>()
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
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Improves usability")]
        public IList<string> DefaultFileNames { get; set; }
    }
}
