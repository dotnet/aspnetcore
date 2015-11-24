// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing
{
    /// <summary>
    /// Represents information about the route and virtual path that are the result of
    /// generating a URL with the ASP.NET routing middleware.
    /// </summary>
    public class VirtualPathData
    {
        private RouteValueDictionary _dataTokens;

        /// <summary>
        ///  Initializes a new instance of the <see cref="VirtualPathData"/> class.
        /// </summary>
        /// <param name="router">The object that is used to generate the URL.</param>
        /// <param name="virtualPath">The generated URL.</param>
        public VirtualPathData(IRouter router, string virtualPath)
            : this(router, virtualPath, dataTokens: null)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="VirtualPathData"/> class.
        /// </summary>
        /// <param name="router">The object that is used to generate the URL.</param>
        /// <param name="virtualPath">The generated URL.</param>
        /// <param name="dataTokens">The collection of custom values.</param>
        public VirtualPathData(
            IRouter router,
            string virtualPath,
            RouteValueDictionary dataTokens)
            : this(router, CreatePathString(virtualPath), dataTokens)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="VirtualPathData"/> class.
        /// </summary>
        /// <param name="router">The object that is used to generate the URL.</param>
        /// <param name="virtualPath">The generated URL.</param>
        /// <param name="dataTokens">The collection of custom values.</param>
        public VirtualPathData(
            IRouter router,
            PathString virtualPath,
            RouteValueDictionary dataTokens)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            Router = router;
            VirtualPath = virtualPath;
            _dataTokens = dataTokens == null ? null : new RouteValueDictionary(dataTokens);
        }

        /// <summary>
        /// Gets the collection of custom values for the <see cref="Router"/>.
        /// </summary>
        public RouteValueDictionary DataTokens
        {
            get
            {
                if (_dataTokens == null)
                {
                    _dataTokens = new RouteValueDictionary();
                }

                return _dataTokens;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IRouter"/> that was used to generate the URL.
        /// </summary>
        public IRouter Router { get; set; }

        /// <summary>
        /// Gets or sets the URL that was generated from the <see cref="Router"/>.
        /// </summary>
        public PathString VirtualPath { get; set; }

        private static PathString CreatePathString(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                PathString pathString;
                if (path.Length > 0 && !path.StartsWith("/", StringComparison.Ordinal))
                {
                    pathString = new PathString("/" + path);
                }
                else
                {
                    pathString = new PathString(path);
                }

                return pathString;
            }

            return PathString.Empty;
        }
    }
}