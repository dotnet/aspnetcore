// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using the request services.
/// </summary>
/// <example>
/// In this example an implementation of IProductModelRequestService is registered as a service.
/// Then in the GetProduct action, the parameter is bound to an instance of IProductModelRequestService
/// which is resolved from the request services.
///
/// <code>
/// [HttpGet]
/// public ProductModel GetProduct([FromServices] IProductModelRequestService productModelRequest)
/// {
///     return productModelRequest.Value;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromServicesAttribute : Attribute, IBindingSourceMetadata, IFromServiceMetadata
{
    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Services;
}
