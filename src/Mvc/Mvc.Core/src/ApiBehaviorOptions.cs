// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Options used to configure behavior for types annotated with <see cref="ApiControllerAttribute"/>.
/// </summary>
public class ApiBehaviorOptions : IEnumerable<ICompatibilitySwitch>
{
    private readonly IReadOnlyList<ICompatibilitySwitch> _switches = Array.Empty<ICompatibilitySwitch>();
    private Func<ActionContext, IActionResult> _invalidModelStateResponseFactory = default!;

    /// <summary>
    /// Delegate invoked on actions annotated with <see cref="ApiControllerAttribute"/> to convert invalid
    /// <see cref="ModelStateDictionary"/> into an <see cref="IActionResult"/>
    /// </summary>
    public Func<ActionContext, IActionResult> InvalidModelStateResponseFactory
    {
        get => _invalidModelStateResponseFactory;
        set => _invalidModelStateResponseFactory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets a value that determines if the filter that returns an <see cref="BadRequestObjectResult"/> when
    /// <see cref="ActionContext.ModelState"/> is invalid is suppressed. <seealso cref="InvalidModelStateResponseFactory"/>.
    /// </summary>
    public bool SuppressModelStateInvalidFilter { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if model binding sources are inferred for action parameters on controllers annotated
    /// with <see cref="ApiControllerAttribute"/> is suppressed.
    /// <para>
    /// When enabled, the following sources are inferred:
    /// Parameters that appear as route values, are assumed to be bound from the path (<see cref="BindingSource.Path"/>).
    /// Parameters of type <see cref="IFormFile"/> and <see cref="IFormFileCollection"/> are assumed to be bound from form.
    /// Parameters that are complex (<see cref="ModelMetadata.IsComplexType"/>) and are registered in the DI Container (<see cref="IServiceCollection"/>) are assumed to be bound from the services <see cref="BindingSource.Services"/>, unless this
    /// option is explicitly disabled <see cref="DisableImplicitFromServicesParameters"/>.
    /// Parameters that are complex (<see cref="ModelMetadata.IsComplexType"/>) are assumed to be bound from the body (<see cref="BindingSource.Body"/>).
    /// All other parameters are assumed to be bound from the query.
    /// </para>
    /// </summary>
    public bool SuppressInferBindingSourcesForParameters { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if parameters are inferred to be from services.
    /// This property is only applicable when <see cref="SuppressInferBindingSourcesForParameters" /> is <see langword="false" />.
    /// </summary>
    public bool DisableImplicitFromServicesParameters { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if an <c>multipart/form-data</c> consumes action constraint is added to parameters
    /// that are bound from form data.
    /// </summary>
    public bool SuppressConsumesConstraintForFormFileParameters { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if controllers with <see cref="ApiControllerAttribute"/>
    /// transform certain client errors.
    /// <para>
    /// When <see langword="false"/>, a result filter is added to API controller actions that transforms
    /// <see cref="IClientErrorActionResult"/>. Otherwise, the filter is suppressed.
    /// </para>
    /// <para>
    /// By default, <see cref="ClientErrorMapping"/> is used to map <see cref="IClientErrorActionResult"/> to a
    /// <see cref="ProblemDetails"/> instance (returned as the value for <see cref="ObjectResult"/>).
    /// </para>
    /// <para>
    /// To customize the output of the filter (for e.g. to return a different error type), register a custom
    /// implementation of <see cref="IClientErrorFactory"/> in the service collection.
    /// </para>
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool SuppressMapClientErrors { get; set; }

    /// <summary>
    /// Gets a map of HTTP status codes to <see cref="ClientErrorData"/>. Configured values
    /// are used to transform <see cref="IClientErrorActionResult"/> to an <see cref="ObjectResult"/>
    /// instance where the <see cref="ObjectResult.Value"/> is <see cref="ProblemDetails"/>.
    /// <para>
    /// Use of this feature can be disabled by resetting <see cref="SuppressMapClientErrors"/>.
    /// </para>
    /// </summary>
    public IDictionary<int, ClientErrorData> ClientErrorMapping { get; } = new Dictionary<int, ClientErrorData>();

    IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator() => _switches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
}
