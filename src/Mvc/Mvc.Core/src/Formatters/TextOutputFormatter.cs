// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// Writes an object in a given text format to the output stream.
/// </summary>
public abstract class TextOutputFormatter : OutputFormatter
{
    private IDictionary<string, string>? _outputMediaTypeCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextOutputFormatter"/> class.
    /// </summary>
    protected TextOutputFormatter()
    {
        SupportedEncodings = new List<Encoding>();
    }

    /// <summary>
    /// Gets the mutable collection of character encodings supported by
    /// this <see cref="TextOutputFormatter"/>. The encodings are
    /// used when writing the data.
    /// </summary>
    public IList<Encoding> SupportedEncodings { get; }

    private IDictionary<string, string> OutputMediaTypeCache
    {
        get
        {
            if (_outputMediaTypeCache == null)
            {
                var cache = new Dictionary<string, string>();
                foreach (var mediaType in SupportedMediaTypes)
                {
                    cache.Add(mediaType, MediaType.ReplaceEncoding(mediaType, Encoding.UTF8));
                }

                // Safe race condition, worst case scenario we initialize the field multiple times with dictionaries containing
                // the same values.
                _outputMediaTypeCache = cache;
            }

            return _outputMediaTypeCache;
        }
    }

    /// <summary>
    /// Determines the best <see cref="Encoding"/> amongst the supported encodings
    /// for reading or writing an HTTP entity body based on the provided content type.
    /// </summary>
    /// <param name="context">The formatter context associated with the call.
    /// </param>
    /// <returns>The <see cref="Encoding"/> to use when reading the request or writing the response.</returns>
    public virtual Encoding SelectCharacterEncoding(OutputFormatterWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (SupportedEncodings.Count == 0)
        {
            var message = Resources.FormatTextOutputFormatter_SupportedEncodingsMustNotBeEmpty(
                nameof(SupportedEncodings));
            throw new InvalidOperationException(message);
        }

        var acceptCharsetHeaderValues = GetAcceptCharsetHeaderValues(context);
        var encoding = MatchAcceptCharacterEncoding(acceptCharsetHeaderValues);
        if (encoding != null)
        {
            return encoding;
        }

        if (context.ContentType.HasValue)
        {
            var parsedContentType = new MediaType(context.ContentType);
            var contentTypeCharset = parsedContentType.Charset;
            if (contentTypeCharset.HasValue)
            {
                for (var i = 0; i < SupportedEncodings.Count; i++)
                {
                    var supportedEncoding = SupportedEncodings[i];
                    if (contentTypeCharset.Equals(supportedEncoding.WebName, StringComparison.OrdinalIgnoreCase))
                    {
                        // This is supported.
                        return SupportedEncodings[i];
                    }
                }
            }
        }

        return SupportedEncodings[0];
    }

    /// <inheritdoc />
    public override Task WriteAsync(OutputFormatterWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var selectedMediaType = context.ContentType;
        if (!selectedMediaType.HasValue)
        {
            // If content type is not set then set it based on supported media types.
            if (SupportedEncodings.Count > 0)
            {
                selectedMediaType = new StringSegment(SupportedMediaTypes[0]);
            }
            else
            {
                throw new InvalidOperationException(Resources.FormatOutputFormatterNoMediaType(GetType().FullName));
            }
        }

        var selectedEncoding = SelectCharacterEncoding(context);
        if (selectedEncoding != null)
        {
            // Override the content type value even if one already existed.
            var mediaTypeWithCharset = GetMediaTypeWithCharset(selectedMediaType.Value!, selectedEncoding);
            selectedMediaType = new StringSegment(mediaTypeWithCharset);
        }
        else
        {
            const int statusCode = StatusCodes.Status406NotAcceptable;
            context.HttpContext.Response.StatusCode = statusCode;

            if (context.HttpContext.RequestServices.GetService<IProblemDetailsService>() is { } problemDetailsService)
            {
                return problemDetailsService.TryWriteAsync(new ()
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = { Status = statusCode }
                }).AsTask();
            }

            return Task.CompletedTask;
        }

        context.ContentType = selectedMediaType;

        WriteResponseHeaders(context);
        return WriteResponseBodyAsync(context, selectedEncoding);
    }

    /// <inheritdoc />
    public sealed override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var message = Resources.FormatTextOutputFormatter_WriteResponseBodyAsyncNotSupported(
            $"{nameof(WriteResponseBodyAsync)}({nameof(OutputFormatterWriteContext)})",
            nameof(TextOutputFormatter),
            $"{nameof(WriteResponseBodyAsync)}({nameof(OutputFormatterWriteContext)},{nameof(Encoding)})");

        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Writes the response body.
    /// </summary>
    /// <param name="context">The formatter context associated with the call.</param>
    /// <param name="selectedEncoding">The <see cref="Encoding"/> that should be used to write the response.</param>
    /// <returns>A task which can write the response body.</returns>
    public abstract Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding);

    internal static IList<StringWithQualityHeaderValue> GetAcceptCharsetHeaderValues(OutputFormatterWriteContext context)
    {
        var request = context.HttpContext.Request;
        if (StringWithQualityHeaderValue.TryParseList(request.Headers.AcceptCharset, out var result))
        {
            return result;
        }

        return Array.Empty<StringWithQualityHeaderValue>();
    }

    private string GetMediaTypeWithCharset(string mediaType, Encoding encoding)
    {
        if (string.Equals(encoding.WebName, Encoding.UTF8.WebName, StringComparison.OrdinalIgnoreCase) &&
            OutputMediaTypeCache.TryGetValue(mediaType, out var mediaTypeWithCharset))
        {
            return mediaTypeWithCharset;
        }

        return MediaType.ReplaceEncoding(mediaType, encoding);
    }

    private Encoding? MatchAcceptCharacterEncoding(IList<StringWithQualityHeaderValue> acceptCharsetHeaders)
    {
        if (acceptCharsetHeaders != null && acceptCharsetHeaders.Count > 0)
        {
            var acceptValues = Sort(acceptCharsetHeaders);
            for (var i = 0; i < acceptValues.Count; i++)
            {
                var charset = acceptValues[i].Value;
                if (!StringSegment.IsNullOrEmpty(charset))
                {
                    for (var j = 0; j < SupportedEncodings.Count; j++)
                    {
                        var encoding = SupportedEncodings[j];
                        if (charset.Equals(encoding.WebName, StringComparison.OrdinalIgnoreCase) ||
                            charset.Equals("*", StringComparison.Ordinal))
                        {
                            return encoding;
                        }
                    }
                }
            }
        }

        return null;
    }

    // There's no allocation-free way to sort an IList and we may have to filter anyway,
    // so we're going to have to live with the copy + insertion sort.
    private static IList<StringWithQualityHeaderValue> Sort(IList<StringWithQualityHeaderValue> values)
    {
        var sortNeeded = false;

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality == HeaderQuality.NoMatch)
            {
                // Exclude this one
            }
            else if (value.Quality != null)
            {
                sortNeeded = true;
            }
        }

        if (!sortNeeded)
        {
            return values;
        }

        var sorted = new List<StringWithQualityHeaderValue>();
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality == HeaderQuality.NoMatch)
            {
                // Exclude this one
            }
            else
            {
                // Doing an insertion sort.
                var position = sorted.BinarySearch(value, StringWithQualityHeaderValueComparer.QualityComparer);
                if (position >= 0)
                {
                    sorted.Insert(position + 1, value);
                }
                else
                {
                    sorted.Insert(~position, value);
                }
            }
        }

        // We want a descending sort, but BinarySearch does ascending
        sorted.Reverse();
        return sorted;
    }
}
