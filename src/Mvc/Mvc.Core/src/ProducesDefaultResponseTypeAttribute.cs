// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A filter that specifies the <see cref="System.Type"/> for all HTTP status codes that are not covered by <see cref="ProducesResponseTypeAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ProducesDefaultResponseTypeAttribute : Attribute, IApiDefaultResponseMetadataProvider
{
    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
    /// </summary>
    public ProducesDefaultResponseTypeAttribute()
        : this(typeof(void))
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
    public ProducesDefaultResponseTypeAttribute(Type type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Gets or sets the type of the value returned by an action.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets or sets the description of the response.
    /// </summary>
    public string? Description { get; set; }

    /// <inheritdoc />
    void IApiResponseMetadataProvider.SetContentTypes(MediaTypeCollection contentTypes)
    {
        // Users are supposed to use the 'Produces' attribute to set the content types that an action can support.
    }
}
