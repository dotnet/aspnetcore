// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.StaticFiles.DirectoryFormatters;
using Microsoft.AspNet.StaticFiles.Infrastructure;

namespace Microsoft.AspNet.StaticFiles
{
    /// <summary>
    /// Directory browsing options
    /// </summary>
    public class DirectoryBrowserOptions : SharedOptionsBase<DirectoryBrowserOptions>
    {
        /// <summary>
        /// Enabled directory browsing in the current physical directory for all request paths
        /// </summary>
        public DirectoryBrowserOptions()
            : this(new SharedOptions())
        {
        }

        /// <summary>
        /// Enabled directory browsing in the current physical directory for all request paths
        /// </summary>
        /// <param name="sharedOptions"></param>
        public DirectoryBrowserOptions(SharedOptions sharedOptions)
            : base(sharedOptions)
        {
            Formatter = new HtmlDirectoryFormatter();
        }

        /// <summary>
        /// The component that generates the view.
        /// </summary>
        public IDirectoryFormatter Formatter { get; set; }
    }
}
