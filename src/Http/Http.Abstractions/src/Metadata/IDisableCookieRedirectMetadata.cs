// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Metadata that indicates the endpoint should disable cookie-based authentication redirects
/// typically because it is intended for API clients rather than direct browser navigation.
///
/// <see cref="IAllowCookieRedirectMetadata"/> overrides this no matter the order.
///
/// When present and not overridden, the cookie authentication handler will prefer using
/// 401 and 403 status codes over redirecting to the login or access denied paths.
/// </summary>
public interface IDisableCookieRedirectMetadata
{
}
