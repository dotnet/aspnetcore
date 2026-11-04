// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// A metadata description of an input to an API.
/// </summary>
public class ApiParameterDescription
{
    /// <summary>
    /// Gets or sets the <see cref="ModelMetadata"/>.
    /// </summary>
    public ModelMetadata ModelMetadata { get; set; } = default!;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="ApiParameterRouteInfo"/>.
    /// </summary>
    public ApiParameterRouteInfo? RouteInfo { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="BindingSource"/>.
    /// </summary>
    public BindingSource Source { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="BindingInfo"/>.
    /// </summary>
    public BindingInfo? BindingInfo { get; set; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public Type Type { get; set; } = default!;

    /// <summary>
    /// Gets or sets the parameter descriptor.
    /// </summary>
    public ParameterDescriptor ParameterDescriptor { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value that determines if the parameter is required.
    /// </summary>
    /// <remarks>
    /// A parameter is considered required if
    /// <list type="bullet">
    /// <item><description>it's bound from the request body (<see cref="BindingSource.Body"/>).</description></item>
    /// <item><description>it's a required route value.</description></item>
    /// <item><description>it has annotations (e.g. BindRequiredAttribute) that indicate it's required.</description></item>
    /// </list>
    /// </remarks>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the default value for a parameter.
    /// </summary>
    public object? DefaultValue { get; set; }
}
