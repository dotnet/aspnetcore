// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using route-data from the current request.
/// </summary>
/// <remarks>
/// Binds a parameter or property to the value of a route parameter with the same name,
/// or the name specified in the <see cref="Name"/> property.
/// The route parameter name is matched against parameter segments of the route pattern case-insensitively.
///
/// For more information about parameter binding see
/// <see href="https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding">Parameter binding</see>.
/// </remarks>
/// <example>
/// In this example, the value of the 'id' route parameter is bound to the parameter.
/// <code>
/// app.MapGet("/from-route/{id}", ([FromRoute] string id) => id);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromRouteAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromRouteMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Path;

    /// <summary>
    /// The <see cref="HttpRequest.RouteValues"/> name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the route parameter value should be fully URL-decoded
    /// using <see cref="Uri.UnescapeDataString(string)"/>.
    /// </summary>
    /// <remarks>
    /// When set to <see langword="true"/>, characters such as <c>%2F</c> (forward slash) that
    /// are normally preserved in route values will be decoded. Defaults to <see langword="false"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapGet("/users/{userId}", ([FromRoute(UrlDecode = true)] string userId) => userId);
    /// </code>
    /// </example>
    public bool UrlDecode { get; set; }
}
