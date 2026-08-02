// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
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

    // We may have to filter q=0 values and reorder the rest by quality. StringWithQualityHeaderValue
    // is a reference type, so it can't live in a stack buffer, but its indices can. Real Accept-Charset
    // headers are tiny (a handful of charsets at most), so for anything within the threshold we sort a
    // stack-allocated inline-array buffer of indices with an insertion sort and never touch the heap for
    // scratch. No real client sends more than a handful of charsets, so the >32 branch only exists for
    // correctness; there we fall back to a plain List.Sort and don't bother preserving the tie ordering,
    // because nothing observable can depend on it.
    private const int SortStackAllocThreshold = 32;

    // Inline-array buffer of SortStackAllocThreshold indices. Preferred over stackalloc because the
    // compiler puts stronger guarantees on inline arrays (no stack cookie, better bounds analysis).
    [InlineArray(SortStackAllocThreshold)]
    private struct IndexBuffer
    {
#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
        private int _element0;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CA1823 // Avoid unused private fields
    }

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

        return values.Count <= SortStackAllocThreshold
            ? SortSmall(values)
            : SortLarge(values);
    }

    // Fast path for realistic headers. We insert the surviving indices into a stack-allocated buffer in
    // descending quality order, filtering out the q=0 rejections as we go. Building descending directly
    // (rather than sorting ascending and reversing) means an already-preferred-first header appends with
    // no shifting, so the common case is O(n); the worst case is an O(n^2) insertion sort, which is
    // irrelevant for at most SortStackAllocThreshold entries. QualityComparer keeps a concrete charset
    // ahead of an equal-quality wildcard and treats two equal-quality concrete values as equal, so
    // shifting past equals (Compare <= 0) lands a later header entry ahead of earlier equals, preserving
    // the previous last-entry-wins selection.
    private static IList<StringWithQualityHeaderValue> SortSmall(IList<StringWithQualityHeaderValue> values)
    {
        var buffer = new IndexBuffer();
        Span<int> indices = buffer;
        var count = 0;

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality == HeaderQuality.NoMatch)
            {
                continue;
            }

            var j = count;
            while (j > 0 &&
                StringWithQualityHeaderValueComparer.QualityComparer.Compare(values[indices[j - 1]], value) <= 0)
            {
                indices[j] = indices[j - 1];
                j--;
            }

            indices[j] = i;
            count++;
        }

        var sorted = new List<StringWithQualityHeaderValue>(count);
        for (var i = 0; i < count; i++)
        {
            sorted.Add(values[indices[i]]);
        }

        return sorted;
    }

    // Pathological path that no real client will ever exercise: more than SortStackAllocThreshold
    // charsets in a single Accept-Charset header. We don't allocate an index buffer or preserve the
    // tie ordering here; a plain List.Sort is unstable, but with this many client-declared charsets
    // there is nothing observable that could depend on how equal-quality values are ordered.
    private static IList<StringWithQualityHeaderValue> SortLarge(IList<StringWithQualityHeaderValue> values)
    {
        var sorted = new List<StringWithQualityHeaderValue>(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value.Quality != HeaderQuality.NoMatch)
            {
                sorted.Add(value);
            }
        }

        // QualityComparer sorts ascending; reverse for the descending order we return.
        sorted.Sort(StringWithQualityHeaderValueComparer.QualityComparer);
        sorted.Reverse();

        return sorted;
    }
}
