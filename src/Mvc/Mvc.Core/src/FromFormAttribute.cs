// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using form-data in the request body.
/// </summary>
/// <remarks>
/// Binds a parameter or property to a field in a form-data request body with the same name,
/// or the name specified in the <see cref="Name"/> property.
/// Form parameter names are matched case-insensitively.
///
/// For more information about parameter binding see
/// <see href="https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding">Parameter binding</see>.
/// </remarks>
/// <example>
/// In this example, the value of the 'name' field in the form-data request body is bound to the name parameter,
/// and the value of the 'age' field is bound to the age parameter.
/// <code>
/// app.MapPost("/from-form", ([FromForm] string name, [FromForm] int age)
///     => new { Name = name, Age = age });
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromFormAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromFormMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Form;

    /// <inheritdoc />
    public string? Name { get; set; }
}
