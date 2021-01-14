// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// An <see cref="EndPoint"/> defined by a <see cref="System.Uri"/>.
    /// </summary>
    public class UriEndPoint : EndPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UriEndPoint"/> class.
        /// </summary>
        /// <param name="uri">The <see cref="System.Uri"/> defining the <see cref="EndPoint"/>.</param>
        public UriEndPoint(Uri uri)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        /// <summary>
        /// The <see cref="System.Uri"/> defining the <see cref="EndPoint"/>.
        /// </summary>
        public Uri Uri { get; }

        public override string ToString() => Uri.ToString();
    }
}
