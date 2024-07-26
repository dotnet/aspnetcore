// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using the request body.
/// </summary>
/// <remarks>
/// By default, ASP.NET Core MVC delegates the responsibility of reading the body to an input formatter.<br/>
/// In the case of ASP.NET Core Minimal APIs, the body is deserialized by <see cref="System.Text.Json.JsonSerializer"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromBodyAttribute : Attribute, IBindingSourceMetadata, IConfigureEmptyBodyBehavior, IFromBodyMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Body;

    /// <summary>
    /// Gets or sets a value which decides whether body model binding should treat empty
    /// input as valid.
    /// </summary>
    /// <remarks>
    /// The default behavior is to use framework defaults as configured by <see cref="MvcOptions.AllowEmptyInputInBodyModelBinding"/>.
    /// Specifying <see cref="EmptyBodyBehavior.Allow"/> or <see cref="EmptyBodyBehavior.Disallow" /> will override the framework defaults.
    /// </remarks>
    public EmptyBodyBehavior EmptyBodyBehavior { get; set; }

    // Since the default behavior is to reject empty bodies if MvcOptions.AllowEmptyInputInBodyModelBinding is not configured,
    // we'll consider EmptyBodyBehavior.Default the same as EmptyBodyBehavior.Disallow.
    bool IFromBodyMetadata.AllowEmpty => EmptyBodyBehavior == EmptyBodyBehavior.Allow;
}
