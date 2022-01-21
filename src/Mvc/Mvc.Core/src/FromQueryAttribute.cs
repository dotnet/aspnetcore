// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using the request query string.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromQueryAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromQueryMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Query;

    /// <inheritdoc />
    public string? Name { get; set; }
}
