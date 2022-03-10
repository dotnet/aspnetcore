// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that returns a Found (302), Moved Permanently (301), Temporary Redirect (307),
/// or Permanent Redirect (308) response with a Location header to the supplied local URL.
/// </summary>
internal sealed partial class LocalRedirectHttpResult : RedirectHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    public LocalRedirectHttpResult(string localUrl)
         : this(localUrl, permanent: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    public LocalRedirectHttpResult(string localUrl, bool permanent)
        : this(localUrl, permanent, preserveMethod: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request's method.</param>
    public LocalRedirectHttpResult(string localUrl, bool permanent, bool preserveMethod)
        : base(localUrl, permanent, preserveMethod, acceptLocalUrlOnly: true)
    {

    }
}
