// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Metadata that indicates the endpoint should allow cookie-based authentication redirects.
/// This is normally the default behavior, but it exists to override <see cref="IDisableCookieRedirectMetadata"/> no matter the order.
/// When present, the cookie authentication handler will prefer browser login or access denied redirects over 401 and 403 status codes.
/// </summary>
public interface IAllowCookieRedirectMetadata
{
}
