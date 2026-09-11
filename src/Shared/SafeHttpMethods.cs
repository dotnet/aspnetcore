// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Helpers for identifying HTTP methods that are "safe" and therefore do not require
/// antiforgery/CSRF validation. GET, HEAD, OPTIONS, and TRACE are safe per RFC 9110;
/// QUERY is defined as a safe, idempotent method by RFC 10008.
/// </summary>
internal static class SafeHttpMethods
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="method"/> is a safe HTTP method
    /// that does not require antiforgery/CSRF validation.
    /// </summary>
    /// <param name="method">The HTTP method name.</param>
    public static bool IsSafe(string method) =>
        HttpMethods.IsGet(method) ||
        HttpMethods.IsHead(method) ||
        HttpMethods.IsOptions(method) ||
        HttpMethods.IsTrace(method) ||
        HttpMethods.IsQuery(method);
}
