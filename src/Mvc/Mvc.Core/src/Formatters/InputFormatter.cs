// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// Reads an object from the request body.
/// </summary>
public abstract class InputFormatter : IInputFormatter, IApiRequestFormatMetadataProvider
{
    /// <summary>
    /// Gets the mutable collection of media type elements supported by
    /// this <see cref="InputFormatter"/>.
    /// </summary>
    public MediaTypeCollection SupportedMediaTypes { get; } = new MediaTypeCollection();

    /// <summary>
    /// Gets the default value for a given type. Used to return a default value when the body contains no content.
    /// </summary>
    /// <param name="modelType">The type of the value.</param>
    /// <returns>The default value for the <paramref name="modelType"/> type.</returns>
    protected virtual object? GetDefaultValueForType(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        if (modelType.IsValueType)
        {
            return Activator.CreateInstance(modelType);
        }

        return null;
    }

    /// <inheritdoc />
    public virtual bool CanRead(InputFormatterContext context)
    {
        if (SupportedMediaTypes.Count == 0)
        {
            var message = Resources.FormatFormatter_NoMediaTypes(
                GetType().FullName,
                nameof(SupportedMediaTypes));

            throw new InvalidOperationException(message);
        }

        if (!CanReadType(context.ModelType))
        {
            return false;
        }

        var contentType = context.HttpContext.Request.ContentType;
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        // Confirm the request's content type is more specific than a media type this formatter supports e.g. OK if
        // client sent "text/plain" data and this formatter supports "text/*".
        return IsSubsetOfAnySupportedContentType(contentType);
    }

    private bool IsSubsetOfAnySupportedContentType(string contentType)
    {
        var parsedContentType = new MediaType(contentType);
        for (var i = 0; i < SupportedMediaTypes.Count; i++)
        {
            var supportedMediaType = new MediaType(SupportedMediaTypes[i]);
            if (parsedContentType.IsSubsetOf(supportedMediaType))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines whether this <see cref="InputFormatter"/> can deserialize an object of the given
    /// <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> of object that will be read.</param>
    /// <returns><c>true</c> if the <paramref name="type"/> can be read, otherwise <c>false</c>.</returns>
    protected virtual bool CanReadType(Type type)
    {
        return true;
    }

    /// <inheritdoc />
    public virtual Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var canHaveBody = context.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
        // In case the feature is not registered
        canHaveBody ??= context.HttpContext.Request.ContentLength != 0;

        if (canHaveBody is false)
        {
            if (context.TreatEmptyInputAsDefaultValue)
            {
                return InputFormatterResult.SuccessAsync(GetDefaultValueForType(context.ModelType));
            }

            return InputFormatterResult.NoValueAsync();
        }

        return ReadRequestBodyAsync(context);
    }

    /// <summary>
    /// Reads an object from the request body.
    /// </summary>
    /// <param name="context">The <see cref="InputFormatterContext"/>.</param>
    /// <returns>A <see cref="Task"/> that on completion deserializes the request body.</returns>
    public abstract Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context);

    /// <inheritdoc />
    public virtual IReadOnlyList<string>? GetSupportedContentTypes(string contentType, Type objectType)
    {
        if (SupportedMediaTypes.Count == 0)
        {
            var message = Resources.FormatFormatter_NoMediaTypes(
                GetType().FullName,
                nameof(SupportedMediaTypes));

            throw new InvalidOperationException(message);
        }

        if (!CanReadType(objectType))
        {
            return null;
        }

        if (contentType == null)
        {
            // If contentType is null, then any type we support is valid.
            return SupportedMediaTypes;
        }
        else
        {
            var parsedContentType = new MediaType(contentType);
            List<string>? mediaTypes = null;

            // Confirm this formatter supports a more specific media type than requested e.g. OK if "text/*"
            // requested and formatter supports "text/plain". Treat contentType like it came from an Content-Type header.
            foreach (var mediaType in SupportedMediaTypes)
            {
                var parsedMediaType = new MediaType(mediaType);
                if (parsedMediaType.IsSubsetOf(parsedContentType))
                {
                    if (mediaTypes == null)
                    {
                        mediaTypes = new List<string>(SupportedMediaTypes.Count);
                    }

                    mediaTypes.Add(mediaType);
                }
            }

            return mediaTypes;
        }
    }
}
