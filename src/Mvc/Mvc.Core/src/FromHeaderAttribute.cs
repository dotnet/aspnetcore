// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using the request headers.
/// </summary>
/// <remarks>
/// Binds a parameter or property to the value of the request header with the same name,
/// or the name specified in the <see cref="Name"/> property.
/// Note that HTTP header names are case-insensitive, so the header name is matched without regard to case.
///
/// For more information about parameter binding see
/// <see href="https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding">Parameter binding</see>.
/// </remarks>
/// <example>
/// In this example, the value of the 'User-Agent' header is bound to the parameter.
/// <code>
/// app.MapGet("/user-agent", ([FromHeader(Name = "User-Agent")] string userAgent)
///     => userAgent);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromHeaderAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromHeaderMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Header;

    /// <inheritdoc />
    public string? Name { get; set; }
}
