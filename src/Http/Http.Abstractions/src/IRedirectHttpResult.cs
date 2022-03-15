// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the HTTP Redirect result of an HTTP result endpoint.
/// </summary>
public interface IRedirectHttpResult : IResult
{
    /// <summary>
    /// Gets the value that specifies that the redirect should be permanent if true or temporary if false.
    /// </summary>
    bool Permanent { get; }

    /// <summary>
    /// Gets an indication that the redirect preserves the initial request method.
    /// </summary>
    bool PreserveMethod { get; }

    /// <summary>
    /// Gets the URL to redirect to.
    /// </summary>
    string? Url { get; }

    /// <summary>
    /// Gets an indication that only local URLs are accepted.
    /// </summary>
    bool AcceptLocalUrlOnly { get; }
}
