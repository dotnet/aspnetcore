// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies the type of the value and status code returned by the action.
/// </summary>
internal sealed class ProducesResponseTypeMetadata : IProducesResponseTypeMetadata
{
    private readonly IEnumerable<string> _contentTypes;

    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeMetadata"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    public ProducesResponseTypeMetadata(int statusCode)
        : this(typeof(void), statusCode, Enumerable.Empty<string>())
    {
    }

    // Only for internal use where validation is unnecessary.
    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeMetadata"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
    /// <param name="statusCode">The HTTP response status code.</param>
    public ProducesResponseTypeMetadata(Type type, int statusCode)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        StatusCode = statusCode;
        _contentTypes = Enumerable.Empty<string>();
    }

    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeMetadata"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
    /// <param name="statusCode">The HTTP response status code.</param>
    /// <param name="contentType">The content type associated with the response.</param>
    /// <param name="additionalContentTypes">Additional content types supported by the response.</param>
    public ProducesResponseTypeMetadata(Type type, int statusCode, string contentType, params string[] additionalContentTypes)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        Type = type ?? throw new ArgumentNullException(nameof(type));
        StatusCode = statusCode;

        MediaTypeHeaderValue.Parse(contentType);
        for (var i = 0; i < additionalContentTypes.Length; i++)
        {
            MediaTypeHeaderValue.Parse(additionalContentTypes[i]);
        }

        _contentTypes = GetContentTypes(contentType, additionalContentTypes);
    }

    // Only for internal use where validation is unnecessary.
    private ProducesResponseTypeMetadata(Type? type, int statusCode, IEnumerable<string> contentTypes)
    {

        Type = type;
        StatusCode = statusCode;
        _contentTypes = contentTypes;
    }

    /// <summary>
    /// Gets or sets the type of the value returned by an action.
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

    public IEnumerable<string> ContentTypes => _contentTypes;

    internal static ProducesResponseTypeMetadata CreateUnvalidated(Type? type, int statusCode, IEnumerable<string> contentTypes) => new(type, statusCode, contentTypes);

    private static List<string> GetContentTypes(string contentType, string[] additionalContentTypes)
    {
        var contentTypes = new List<string>(additionalContentTypes.Length + 1);
        ValidateContentType(contentType);
        contentTypes.Add(contentType);
        foreach (var type in additionalContentTypes)
        {
            ValidateContentType(type);
            contentTypes.Add(type);
        }

        return contentTypes;

        static void ValidateContentType(string type)
        {
            if (type.Contains('*', StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Could not parse '{type}'. Content types with wildcards are not supported.");
            }
        }
    }
}
