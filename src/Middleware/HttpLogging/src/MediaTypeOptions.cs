// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Options for HttpLogging to configure which encoding to use for each media type.
/// </summary>
public sealed class MediaTypeOptions
{
    private readonly List<MediaTypeState> _mediaTypeStates = new();

    internal MediaTypeOptions()
    {
    }

    internal List<MediaTypeState> MediaTypeStates => _mediaTypeStates;

    internal static MediaTypeOptions BuildDefaultMediaTypeOptions()
    {
        var options = new MediaTypeOptions();
        options.AddText("application/json", Encoding.UTF8);
        options.AddText("application/*+json", Encoding.UTF8);
        options.AddText("application/xml", Encoding.UTF8);
        options.AddText("application/*+xml", Encoding.UTF8);
        options.AddText("text/*", Encoding.UTF8);

        return options;
    }

    internal void AddText(MediaTypeHeaderValue mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);

        mediaType.Encoding ??= Encoding.UTF8;

        _mediaTypeStates.Add(new MediaTypeState(mediaType) { Encoding = mediaType.Encoding });
    }

    /// <summary>
    /// Adds a contentType to be used for logging as text.
    /// </summary>
    /// <remarks>
    /// If charset is not specified in the contentType, the encoding will default to UTF-8.
    /// </remarks>
    /// <param name="contentType">The content type to add.</param>
    public void AddText(string contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        AddText(MediaTypeHeaderValue.Parse(contentType));
    }

    /// <summary>
    /// Adds a contentType to be used for logging as text.
    /// </summary>
    /// <param name="contentType">The content type to add.</param>
    /// <param name="encoding">The encoding to use.</param>
    public void AddText(string contentType, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(encoding);

        var mediaType = MediaTypeHeaderValue.Parse(contentType);
        mediaType.Encoding = encoding;
        AddText(mediaType);
    }

    /// <summary>
    /// Adds a <see cref="MediaTypeHeaderValue"/> to be used for logging as binary.
    /// </summary>
    /// <param name="mediaType">The MediaType to add.</param>
    public void AddBinary(MediaTypeHeaderValue mediaType)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Adds a content to be used for logging as text.
    /// </summary>
    /// <param name="contentType">The content type to add.</param>
    public void AddBinary(string contentType)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Clears all MediaTypes.
    /// </summary>
    public void Clear()
    {
        _mediaTypeStates.Clear();
    }

    internal readonly struct MediaTypeState
    {
        public MediaTypeState(MediaTypeHeaderValue mediaTypeHeaderValue)
        {
            MediaTypeHeaderValue = mediaTypeHeaderValue;
            Encoding = null;
            IsBinary = false;
        }

        public MediaTypeHeaderValue MediaTypeHeaderValue { get; }
        public Encoding? Encoding { get; init; }
        public bool IsBinary { get; init; }
    }
}
