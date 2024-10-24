// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using the request headers.
/// </summary>
/// <remarks>
/// When placed on a parameter, the parameter will be bound to the value of the request header with the same name.
/// Use the <see cref="Name"/> property to specify a different header name.
/// </remarks>
/// <example>
/// In this example, the value of the 'User-Agent' header is bound to the parameter.
/// <code>
/// [HttpGet]
/// public string GetUserAgent([FromHeader("User-Agent")] string userAgent)
/// </code>
/// Note that HTTP header names are case-insensitive, so the header name is matched without regard to case.
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromHeaderAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromHeaderMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Header;

    /// <inheritdoc />
    public string? Name { get; set; }
}
