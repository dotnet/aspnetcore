// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Represents an API exposed by this application.
/// </summary>
[DebuggerDisplay("{ActionDescriptor.DisplayName,nq}")]
public class ApiDescription
{
    /// <summary>
    /// Gets or sets <see cref="ActionDescriptor"/> for this api.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; set; } = default!;

    /// <summary>
    /// Gets or sets group name for this api.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets the supported HTTP method for this api, or null if all HTTP methods are supported.
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Gets a list of <see cref="ApiParameterDescription"/> for this api.
    /// </summary>
    public IList<ApiParameterDescription> ParameterDescriptions { get; } = new List<ApiParameterDescription>();

    /// <summary>
    /// Gets arbitrary metadata properties associated with the <see cref="ApiDescription"/>.
    /// </summary>
    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    /// <summary>
    /// Gets or sets relative url path template (relative to application root) for this api.
    /// </summary>
    public string? RelativePath { get; set; }

    /// <summary>
    /// Gets the list of possible formats for a request.
    /// </summary>
    /// <remarks>
    /// Will be empty if the action does not accept a parameter decorated with the <c>[FromBody]</c> attribute.
    /// </remarks>
    public IList<ApiRequestFormat> SupportedRequestFormats { get; } = new List<ApiRequestFormat>();

    /// <summary>
    /// Gets the list of possible formats for a response.
    /// </summary>
    /// <remarks>
    /// Will be empty if the action returns no response, or if the response type is unclear. Use
    /// <c>ProducesAttribute</c> on an action method to specify a response type.
    /// </remarks>
    public IList<ApiResponseType> SupportedResponseTypes { get; } = new List<ApiResponseType>();
}
