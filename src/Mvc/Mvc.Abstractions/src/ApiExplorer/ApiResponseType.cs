// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Possible type of the response body which is formatted by <see cref="ApiResponseFormats"/>.
/// </summary>
public class ApiResponseType
{
    /// <summary>
    /// Gets or sets the response formats supported by this type.
    /// </summary>
    public IList<ApiResponseFormat> ApiResponseFormats { get; set; } = new List<ApiResponseFormat>();

    /// <summary>
    /// Gets or sets <see cref="ModelBinding.ModelMetadata"/> for the <see cref="Type"/> or null.
    /// </summary>
    /// <remarks>
    /// Will be null if <see cref="Type"/> is null or void.
    /// </remarks>
    public ModelMetadata? ModelMetadata { get; set; }

    /// <summary>
    /// Gets or sets the CLR data type of the response or null.
    /// </summary>
    /// <remarks>
    /// Will be null if the action returns no response, or if the response type is unclear. Use
    /// <c>Microsoft.AspNetCore.Mvc.ProducesAttribute</c> or <c>Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute</c> on an action method
    /// to specify a response type.
    /// </remarks>
    public Type? Type { get; set; }

    /// <summary>
    /// Gets or sets the description of the response.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the HTTP response status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response type represents a default response.
    /// </summary>
    /// <remarks>
    /// If an <see cref="ApiDescription"/> has a default response, then the <see cref="StatusCode"/> property should be ignored. This response
    /// will be used when a more specific response format does not apply. The common use of a default response is to specify the format
    /// for communicating error conditions.
    /// </remarks>
    public bool IsDefaultResponse { get; set; }
}
