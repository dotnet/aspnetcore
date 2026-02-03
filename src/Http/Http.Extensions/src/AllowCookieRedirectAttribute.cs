// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies that cookie-based authentication redirects are allowed for an endpoint.
/// This is normally the default behavior, but it exists to override <see cref="IDisableCookieRedirectMetadata"/> no matter the order.
/// When present, the cookie authentication handler will prefer browser login or access denied redirects over 401 and 403 status codes.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowCookieRedirectAttribute : Attribute, IAllowCookieRedirectMetadata
{
}
