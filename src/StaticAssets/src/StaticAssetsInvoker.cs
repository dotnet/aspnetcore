// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.StaticAssets;

internal class StaticAssetsInvoker
{
    private readonly StaticAssetDescriptor _resource;
    private readonly IFileProvider _fileProvider;
    private readonly ILogger _logger;
    private readonly string? _contentType;

    private readonly EntityTagHeaderValue _etag;
    private readonly long _length;
    private readonly DateTimeOffset _lastModified;
    private readonly List<StaticAssetResponseHeader> _remainingHeaders;

    private IFileInfo? _fileInfo;

    public StaticAssetsInvoker(StaticAssetDescriptor resource, IFileProvider fileProvider, ILogger<StaticAssetsInvoker> logger)
    {
        _resource = resource;
        _fileProvider = fileProvider;
        _logger = logger;
        _remainingHeaders ??= [];

        foreach (var responseHeader in resource.ResponseHeaders)
        {
            switch (responseHeader)
            {
                case { Name: "Content-Type", Value: var contentType }:
                    _contentType = contentType;
                    break;
                case { Name: "ETag", Value: var etag }:
                    if (_etag == null || _etag.IsWeak)
                    {
                        if (_etag != null)
                        {
                            _remainingHeaders.Add(new StaticAssetResponseHeader("ETag", _etag.ToString()));
                        }

                        _etag = EntityTagHeaderValue.Parse(etag);
                        break;
                    }
                    else
                    {
                        goto default;
                    }
                case { Name: "Last-Modified", Value: var lastModified }:
                    _lastModified = DateTimeOffset.Parse(lastModified, CultureInfo.InvariantCulture);
                    break;
                case { Name: "Content-Length", Value: var length }:
                    _length = long.Parse(length, CultureInfo.InvariantCulture);
                    break;
                default:
                    _remainingHeaders ??= [];
                    _remainingHeaders.Add(responseHeader);
                    break;
            }
        }

        if (_etag == null)
        {
            throw new InvalidOperationException("The ETag header is required.");
        }
    }

    public string Route => _resource.Route;

    public string PhysicalPath => FileInfo.PhysicalPath ?? string.Empty;

    public IFileInfo FileInfo => _fileInfo ??=
        _fileProvider.GetFileInfo(_resource.AssetPath) is IFileInfo file and { Exists: true } ?
        file :
        throw new InvalidOperationException($"The file '{_resource.AssetPath}' could not be found.");

    private Task ApplyResponseHeadersAsync(StaticAssetInvocationContext context, int statusCode)
    {
        if (statusCode < 400)
        {
            // these headers are returned for 200, 206, and 304
            // they are not returned for 412 and 416
            if (!string.IsNullOrEmpty(_contentType))
            {
                context.Response.ContentType = _contentType;
            }

            var responseHeaders = context.ResponseHeaders;
            responseHeaders.LastModified = _lastModified;
            responseHeaders.ETag = _etag;
            responseHeaders.Headers.AcceptRanges = "bytes";

            foreach (var header in _remainingHeaders ?? [])
            {
                responseHeaders.Append(header.Name, header.Value);
            }
        }

        return Task.CompletedTask;
    }

    private Task SendStatusAsync(StaticAssetInvocationContext context, int statusCode)
    {
        _logger.Handled(statusCode, Route);

        // Only clobber the default status (e.g. in cases this a status code pages retry)
        if (context.Response.StatusCode == StatusCodes.Status200OK)
        {
            context.Response.StatusCode = statusCode;
        }

        return ApplyResponseHeadersAsync(context, statusCode);
    }

    public async Task Invoke(HttpContext context)
    {
        var requestContext = new StaticAssetInvocationContext(
            context,
            _etag,
            _lastModified,
            _length,
            _logger);

        var (preconditionState, isRange, range) = requestContext.ComprehendRequestHeaders();
        switch (preconditionState)
        {
            case PreconditionState.Unspecified:
            case PreconditionState.ShouldProcess:
                if (HttpMethods.IsHead(context.Request.Method))
                {
                    await SendStatusAsync(requestContext, StatusCodes.Status200OK);
                    return;
                }

                try
                {
                    if (isRange)
                    {
                        await SendRangeAsync(requestContext, range);
                        return;
                    }

                    context.Response.ContentLength = _length;

                    await SendAsync(requestContext);
                    _logger.FileServed(Route, PhysicalPath);
                    return;
                }
                catch (FileNotFoundException)
                {
                    context.Response.Clear();
                }
                return;
            case PreconditionState.NotModified:
                _logger.FileNotModified(Route);
                await SendStatusAsync(requestContext, StatusCodes.Status304NotModified);
                return;
            case PreconditionState.PreconditionFailed:
                _logger.PreconditionFailed(Route);
                await SendStatusAsync(requestContext, StatusCodes.Status412PreconditionFailed);
                return;
            default:
                var exception = new NotImplementedException(preconditionState.ToString());
                Debug.Fail(exception.ToString());
                throw exception;
        }
    }

    private async Task SendAsync(StaticAssetInvocationContext context)
    {
        await ApplyResponseHeadersAsync(context, StatusCodes.Status200OK);
        try
        {
            await context.Response.SendFileAsync(FileInfo, 0, _length, context.CancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            // Don't throw this exception, it's most likely caused by the client disconnecting.
            _logger.WriteCancelled(ex);
        }
    }

    // When there is only a single range the bytes are sent directly in the body.
    private async Task SendRangeAsync(StaticAssetInvocationContext requestContext, RangeItemHeaderValue? range)
    {
        if (range == null)
        {
            // 14.16 Content-Range - A server sending a response with status code 416 (Requested range not satisfiable)
            // SHOULD include a Content-Range field with a byte-range-resp-spec of "*". The instance-length specifies
            // the current length of the selected resource.  e.g. */length
            requestContext.ResponseHeaders.ContentRange = new ContentRangeHeaderValue(_length);
            if (requestContext.Response.StatusCode == StatusCodes.Status200OK)
            {
                requestContext.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
            }

            _logger.RangeNotSatisfiable(Route);
            return;
        }

        requestContext.ResponseHeaders.ContentRange = ComputeContentRange(range, out var start, out var length);
        requestContext.Response.ContentLength = length;

        if (requestContext.Response.StatusCode == StatusCodes.Status200OK)
        {
            requestContext.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
        }
        await ApplyResponseHeadersAsync(requestContext, StatusCodes.Status206PartialContent);

        try
        {
            var logPath = !string.IsNullOrEmpty(FileInfo.PhysicalPath) ? FileInfo.PhysicalPath : Route;
            _logger.SendingFileRange(requestContext.Response.Headers.ContentRange, logPath);
            await requestContext.Response.SendFileAsync(FileInfo, start, length, requestContext.CancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            // Don't throw this exception, it's most likely caused by the client disconnecting.
            _logger.WriteCancelled(ex);
        }
    }

    // Note: This assumes ranges have been normalized to absolute byte offsets.
    private ContentRangeHeaderValue ComputeContentRange(RangeItemHeaderValue range, out long start, out long length)
    {
        start = range.From!.Value;
        var end = range.To!.Value;
        length = end - start + 1;
        return new ContentRangeHeaderValue(start, end, _length);
    }

    private readonly struct StaticAssetInvocationContext
    {
        private readonly HttpContext _context = null!;
        private readonly HttpRequest _request = null!;
        private readonly EntityTagHeaderValue _etag;
        private readonly DateTimeOffset _lastModified;
        private readonly long _length;
        private readonly ILogger _logger;
        private readonly RequestHeaders _requestHeaders;

        public StaticAssetInvocationContext(
            HttpContext context,
            EntityTagHeaderValue entityTag,
            DateTimeOffset lastModified,
            long length,
            ILogger logger)
        {
            _context = context;
            _request = context.Request;
            ResponseHeaders = context.Response.GetTypedHeaders();
            _requestHeaders = _request.GetTypedHeaders();
            Response = context.Response;
            _etag = entityTag;
            _lastModified = lastModified;
            _length = length;
            _logger = logger;
        }

        public CancellationToken CancellationToken => _context.RequestAborted;

        public ResponseHeaders ResponseHeaders { get; }

        public HttpResponse Response { get; }

        public (PreconditionState, bool isRange, RangeItemHeaderValue? range) ComprehendRequestHeaders()
        {
            var (ifMatch, ifNoneMatch) = ComputeIfMatch();
            var (ifModifiedSince, ifUnmodifiedSince) = ComputeIfModifiedSince();

            var (isRange, range) = ComputeRange();

            isRange = ComputeIfRange(isRange);

            return (GetPreconditionState(ifMatch, ifNoneMatch, ifModifiedSince, ifUnmodifiedSince), isRange, range);
        }

        private (PreconditionState ifMatch, PreconditionState ifNoneMatch) ComputeIfMatch()
        {
            var requestHeaders = _requestHeaders;
            var ifMatchResult = PreconditionState.Unspecified;

            // 14.24 If-Match
            var ifMatch = requestHeaders.IfMatch;
            if (ifMatch?.Count > 0)
            {
                ifMatchResult = PreconditionState.PreconditionFailed;
                foreach (var etag in ifMatch)
                {
                    if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(_etag, useStrongComparison: false))
                    {
                        ifMatchResult = PreconditionState.ShouldProcess;
                        break;
                    }
                }
            }

            // 14.26 If-None-Match
            var ifNoneMatchResult = PreconditionState.Unspecified;
            var ifNoneMatch = requestHeaders.IfNoneMatch;
            if (ifNoneMatch?.Count > 0)
            {
                ifNoneMatchResult = PreconditionState.ShouldProcess;
                foreach (var etag in ifNoneMatch)
                {
                    if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(_etag, useStrongComparison: false))
                    {
                        ifNoneMatchResult = PreconditionState.NotModified;
                        break;
                    }
                }
            }

            return (ifMatchResult, ifNoneMatchResult);
        }

        private (PreconditionState ifModifiedSince, PreconditionState ifUnmodifiedSince) ComputeIfModifiedSince()
        {
            var requestHeaders = _requestHeaders;
            var now = DateTimeOffset.UtcNow;

            // 14.25 If-Modified-Since
            var ifModifiedSinceResult = PreconditionState.Unspecified;
            var ifModifiedSince = requestHeaders.IfModifiedSince;
            if (ifModifiedSince.HasValue && ifModifiedSince <= now)
            {
                var modified = ifModifiedSince < _lastModified;
                ifModifiedSinceResult = modified ? PreconditionState.ShouldProcess : PreconditionState.NotModified;
            }

            // 14.28 If-Unmodified-Since
            var ifUnmodifiedSinceResult = PreconditionState.Unspecified;
            var ifUnmodifiedSince = requestHeaders.IfUnmodifiedSince;
            if (ifUnmodifiedSince.HasValue && ifUnmodifiedSince <= now)
            {
                var unmodified = ifUnmodifiedSince >= _lastModified;
                ifUnmodifiedSinceResult = unmodified ? PreconditionState.ShouldProcess : PreconditionState.PreconditionFailed;
            }

            return (ifModifiedSinceResult, ifUnmodifiedSinceResult);
        }

        private bool ComputeIfRange(bool isRange)
        {
            // 14.27 If-Range
            var ifRangeHeader = _requestHeaders.IfRange;
            if (ifRangeHeader != null)
            {
                // If the validator given in the If-Range header field matches the
                // current validator for the selected representation of the target
                // resource, then the server SHOULD process the Range header field as
                // requested.  If the validator does not match, the server MUST ignore
                // the Range header field.
                if (ifRangeHeader.LastModified.HasValue)
                {
                    if (_lastModified > ifRangeHeader.LastModified)
                    {
                        isRange = false;
                    }
                }
                else if (_etag != null && ifRangeHeader.EntityTag != null && !ifRangeHeader.EntityTag.Compare(_etag, useStrongComparison: true))
                {
                    isRange = false;
                }
            }

            return isRange;
        }

        private (bool isRangeRequest, RangeItemHeaderValue? range) ComputeRange()
        {
            // 14.35 Range
            // http://tools.ietf.org/html/draft-ietf-httpbis-p5-range-24

            // A server MUST ignore a Range header field received with a request method other
            // than GET.
            if (!HttpMethods.IsGet(_request.Method))
            {
                return default;
            }

            (var isRangeRequest, var range) = RangeHelper.ParseRange(_context, _requestHeaders, _length, _logger);

            return (isRangeRequest, range);
        }

        public static PreconditionState GetPreconditionState(
            PreconditionState ifMatchState,
            PreconditionState ifNoneMatchState,
            PreconditionState ifModifiedSinceState,
            PreconditionState ifUnmodifiedSinceState)
        {
            Span<PreconditionState> states = [ifMatchState, ifNoneMatchState, ifModifiedSinceState, ifUnmodifiedSinceState];
            var max = PreconditionState.Unspecified;
            for (var i = 0; i < states.Length; i++)
            {
                if (states[i] > max)
                {
                    max = states[i];
                }
            }
            return max;
        }
    }

    internal enum PreconditionState : byte
    {
        Unspecified,
        NotModified,
        ShouldProcess,
        PreconditionFailed
    }

    [Flags]
    private enum RequestType : byte
    {
        Unspecified = 0b_000,
        IsHead = 0b_001,
        IsGet = 0b_010,
        IsRange = 0b_100,
    }
}
