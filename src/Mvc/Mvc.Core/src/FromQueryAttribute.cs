// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using the request query string.
/// </summary>
/// <remarks>
/// Binds a parameter or property to the value of query string parameter with the same name,
/// or the name specified in the <see cref="Name"/> property.
/// The query parameter name is matched case-insensitively.
///
/// For more information about parameter binding see
/// <see href="https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding">Parameter binding</see>.
/// </remarks>
/// <example>
/// In this example, the value of the 'User-Agent' header is bound to the parameter.
/// <code>
/// app.MapGet("/version", ([FromQuery(Name = "api-version")] string apiVersion)
///     => apiVersion);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromQueryAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromQueryMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Query;

    /// <inheritdoc />
    public string? Name { get; set; }
}
