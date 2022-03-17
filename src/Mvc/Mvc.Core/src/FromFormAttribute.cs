// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using form-data in the request body.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromFormAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromFormMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Form;

    /// <inheritdoc />
    public string? Name { get; set; }
}
