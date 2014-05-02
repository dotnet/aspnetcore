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

using System;
using Microsoft.AspNet.StaticFiles.ContentTypes;
using Microsoft.AspNet.StaticFiles.Infrastructure;

namespace Microsoft.AspNet.StaticFiles
{
    /// <summary>
    /// Options for serving static files
    /// </summary>
    public class StaticFileOptions : SharedOptionsBase<StaticFileOptions>
    {
        /// <summary>
        /// Defaults to all request paths in the current physical directory
        /// </summary>
        public StaticFileOptions() : this(new SharedOptions())
        {
        }

        /// <summary>
        /// Defaults to all request paths in the current physical directory
        /// </summary>
        /// <param name="sharedOptions"></param>
        public StaticFileOptions(SharedOptions sharedOptions) : base(sharedOptions)
        {
            ContentTypeProvider = new FileExtensionContentTypeProvider();

            OnPrepareResponse = _ => { };
        }

        /// <summary>
        /// Used to map files to content-types.
        /// </summary>
        public IContentTypeProvider ContentTypeProvider { get; set; }

        /// <summary>
        /// The default content type for a request if the ContentTypeProvider cannot determine one.
        /// None is provided by default, so the client must determine the format themselves.
        /// http://www.w3.org/Protocols/rfc2616/rfc2616-sec7.html#sec7
        /// </summary>
        public string DefaultContentType { get; set; }

        /// <summary>
        /// If the file is not a recognized content-type should it be served?
        /// Default: false.
        /// </summary>
        public bool ServeUnknownFileTypes { get; set; }

        /// <summary>
        /// Called after the status code and headers have been set, but before the body has been written.
        /// This can be used to add or change the response headers.
        /// </summary>
        public Action<StaticFileResponseContext> OnPrepareResponse { get; set; }
    }
}
