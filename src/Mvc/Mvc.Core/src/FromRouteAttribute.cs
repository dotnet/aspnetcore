// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using route-data from the current request.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromRouteAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromRouteMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Path;

    /// <summary>
    /// The <see cref="HttpRequest.RouteValues"/> name.
    /// </summary>
    public string? Name { get; set; }
}
