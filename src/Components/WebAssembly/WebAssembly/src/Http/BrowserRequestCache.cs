// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Http;

/// <summary>
/// The cache mode of the request. It controls how the request will interact with the browser's HTTP cache.
/// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/cache"/>.
/// </summary>
public enum BrowserRequestCache
{
    /// <summary>
    /// The browser looks for a matching request in its HTTP cache.
    /// </summary>
    Default,

    /// <summary>
    /// The browser fetches the resource from the remote server without first looking in the cache,
    /// and will not update the cache with the downloaded resource.
    /// </summary>
    NoStore,

    /// <summary>
    /// The browser fetches the resource from the remote server without first looking in the cache,
    /// but then will update the cache with the downloaded resource.
    /// </summary>
    Reload,

    /// <summary>
    /// The browser looks for a matching request in its HTTP cache.
    /// <para>
    /// If there is a match, fresh or stale, the browser will make a conditional request to the remote server.
    /// If the server indicates that the resource has not changed, it will be returned from the cache.
    /// Otherwise the resource will be downloaded from the server and the cache will be updated.
    /// </para>
    /// <para>
    /// If there is no match, the browser will make a normal request, and will update the cache with the downloaded resource.
    /// </para>
    /// </summary>
    NoCache,

    /// <summary>
    /// The browser looks for a matching request in its HTTP cache.
    /// <para>
    /// If there is a match, fresh or stale, it will be returned from the cache.
    /// </para>
    /// <para>
    /// If there is no match, the browser will make a normal request, and will update the cache with the downloaded resource.
    /// </para>
    /// </summary>
    ForceCache,

    /// <summary>
    /// The browser looks for a matching request in its HTTP cache.
    /// Mode can only be used if the request's mode is "same-origin"
    /// <para>
    /// If there is a match, fresh or stale, it will be returned from the cache.
    /// </para>
    /// <para>
    /// If there is no match, the browser will respond with a 504 Gateway timeout status.
    /// </para>
    /// </summary>
    OnlyIfCached,
}
