// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.StaticFiles.Infrastructure
{
    /// <summary>
    /// Options common to several middleware components
    /// </summary>
    public abstract class SharedOptionsBase
    {
        /// <summary>
        /// Creates an new instance of the SharedOptionsBase.
        /// </summary>
        /// <param name="sharedOptions"></param>
        protected SharedOptionsBase(SharedOptions sharedOptions)
        {
            if (sharedOptions == null)
            {
                throw new ArgumentNullException(nameof(sharedOptions));
            }

            SharedOptions = sharedOptions;
        }

        /// <summary>
        /// Options common to several middleware components
        /// </summary>
        protected SharedOptions SharedOptions { get; private set; }

        /// <summary>
        /// The relative request path that maps to static resources.
        /// </summary>
        public PathString RequestPath
        {
            get { return SharedOptions.RequestPath; }
            set { SharedOptions.RequestPath = value; }
        }

        /// <summary>
        /// The file system used to locate resources
        /// </summary>
        public IFileProvider FileProvider
        {
            get { return SharedOptions.FileProvider; }
            set { SharedOptions.FileProvider = value; }
        }

        /// <summary>
        /// Indicates whether to redirect to add a trailing slash at the end of path. Relative resource links may require this.
        /// </summary>
        public bool RedirectToAppendTrailingSlash
        {
            get { return SharedOptions.RedirectToAppendTrailingSlash; }
            set { SharedOptions.RedirectToAppendTrailingSlash = value; }
        }
    }
}
