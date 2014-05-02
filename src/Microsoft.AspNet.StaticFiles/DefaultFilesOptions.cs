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
