// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Shared;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies the type of the value and status code returned by the action.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ProducesResponseTypeMetadata : IProducesResponseTypeMetadata
{
    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeMetadata"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
    /// <param name="contentTypes">Content types supported by the response.</param>
    /// <param name="description">The description of the response.</param>
    public ProducesResponseTypeMetadata(int statusCode, Type? type = null, string[]? contentTypes = null, string? description = null)
    {
        StatusCode = statusCode;
        Type = type;
        Description = description;

        if (contentTypes is null || contentTypes.Length == 0)
        {
            ContentTypes = Enumerable.Empty<string>();
        }
        else
        {
            for (var i = 0; i < contentTypes.Length; i++)
            {
                MediaTypeHeaderValue.Parse(contentTypes[i]);
                ValidateContentType(contentTypes[i]);
            }

            ContentTypes = contentTypes;
        }

        static void ValidateContentType(string type)
        {
            if (type.Contains('*', StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Could not parse '{type}'. Content types with wildcards are not supported.");
            }
        }
    }

    // 9.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeMetadata"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
    /// <param name="contentTypes">Content types supported by the response.</param>
    public ProducesResponseTypeMetadata(int statusCode, Type? type = null, string[]? contentTypes = null) : this(statusCode, type, contentTypes, description: null) { }

    // Only for internal use where validation is unnecessary.
    private ProducesResponseTypeMetadata(int statusCode, Type? type, IEnumerable<string> contentTypes, string? description = null)
    {
        Type = type;
        StatusCode = statusCode;
        ContentTypes = contentTypes;
        Description = description;
    }

    /// <summary>
    /// Gets or sets the type of the value returned by an action.
    /// </summary>
    public Type? Type { get; private set; }

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; private set; }

    /// <summary>
    /// Gets or sets the description of the response.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets or sets the content types associated with the response.
    /// </summary>
    public IEnumerable<string> ContentTypes { get; private set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(StatusCode), StatusCode, nameof(ContentTypes), ContentTypes, nameof(Type), Type, includeNullValues: false, prefix: "Produces");
    }

    internal static ProducesResponseTypeMetadata CreateUnvalidated(Type? type, int statusCode, IEnumerable<string> contentTypes, string? description) => new(statusCode, type, contentTypes, description);
}
