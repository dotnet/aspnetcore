// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.StaticFiles.Infrastructure
{
    /// <summary>
    /// Options common to several middleware components
    /// </summary>
    /// <typeparam name="T">The type of the subclass</typeparam>
    public abstract class SharedOptionsBase<T>
    {
        /// <summary>
        /// Creates an new instance of the SharedOptionsBase.
        /// </summary>
        /// <param name="sharedOptions"></param>
        protected SharedOptionsBase(SharedOptions sharedOptions)
        {
            if (sharedOptions == null)
            {
                throw new ArgumentNullException("sharedOptions");
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
        public IFileSystem FileSystem
        {
            get { return SharedOptions.FileSystem; }
            set { SharedOptions.FileSystem = value; }
        }
    }
}
