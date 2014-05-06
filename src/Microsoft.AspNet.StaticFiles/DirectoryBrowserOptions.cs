// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
