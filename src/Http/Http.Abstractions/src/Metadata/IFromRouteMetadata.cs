// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Interface marking attributes that specify a parameter should be bound using route-data from the current request.
/// </summary>
public interface IFromRouteMetadata
{
    /// <summary>
    /// The <see cref="HttpRequest.RouteValues"/> name.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets a value indicating whether the route parameter value should be fully URL-decoded
    /// using <see cref="System.Uri.UnescapeDataString(string)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, some characters such as <c>/</c> (<c>%2F</c>) are not decoded
    /// in route values because they are decoded at the server level with special handling.
    /// Setting this property to <c>true</c> ensures the value is fully percent-decoded
    /// per RFC 3986.
    /// </para>
    /// <example>
    /// <code>
    /// app.MapGet("/users/{userId}", ([FromRoute(UrlDecode = true)] string userId) => userId);
    /// </code>
    /// </example>
    /// </remarks>
    bool UrlDecode => false;
}
