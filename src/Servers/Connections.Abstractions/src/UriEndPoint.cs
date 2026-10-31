// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;

namespace Microsoft.AspNetCore.Connections;

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

    /// <inheritdoc/>
    public override string ToString() => Uri.ToString();
}
