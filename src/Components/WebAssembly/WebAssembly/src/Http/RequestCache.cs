// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Http
{
    /// <summary>
    /// The cache mode of the request. It controls how the request will interact with the browser's HTTP cache.
    /// </summary>
    public enum RequestCache
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
        /// </summary>
        NoCache,

        /// <summary>
        /// The browser looks for a matching request in its HTTP cache.
        /// </summary>
        ForceCache,

        /// <summary>
        /// The browser looks for a matching request in its HTTP cache.
        /// Mode can only be used if the request's mode is "same-origin"
        /// </summary>
        OnlyIfCached,
    }
}
