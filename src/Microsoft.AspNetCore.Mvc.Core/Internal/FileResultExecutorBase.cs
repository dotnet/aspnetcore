// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileResultExecutorBase
    {
        private const string AcceptRangeHeaderValue = "bytes";

        // default buffer size as defined in BufferedStream type
        protected const int BufferSize = 0x1000;

        public FileResultExecutorBase(ILogger logger)
        {
            Logger = logger;
        }

        internal enum PreconditionState
        {
            Unspecified,
            NotModified,
            ShouldProcess,
            PreconditionFailed,
            IgnoreRangeRequest
        }

        protected ILogger Logger { get; }

        protected virtual (RangeItemHeaderValue range, long rangeLength, bool serveBody) SetHeadersAndLog(
            ActionContext context,
            FileResult result, long?
            fileLength, DateTimeOffset?
            lastModified = null,
            EntityTagHeaderValue etag = null)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            SetContentType(context, result);
            SetContentDispositionHeader(context, result);
            Logger.FileResultExecuting(result.FileDownloadName);
            if (fileLength.HasValue)
            {
                SetAcceptRangeHeader(context);
            }

            var request = context.HttpContext.Request;
            var httpRequestHeaders = request.GetTypedHeaders();
            var response = context.HttpContext.Response;
            var httpResponseHeaders = response.GetTypedHeaders();
            if (lastModified.HasValue)
            {
                httpResponseHeaders.LastModified = lastModified;
            }
            if (etag != null)
            {
                httpResponseHeaders.ETag = etag;
            }

            var serveBody = !HttpMethods.IsHead(request.Method);
            if (HttpMethods.IsHead(request.Method) || HttpMethods.IsGet(request.Method))
            {
                var preconditionState = GetPreconditionState(context, httpRequestHeaders, lastModified, etag);
                if (request.Headers.ContainsKey(HeaderNames.Range) &&
                    (preconditionState == PreconditionState.Unspecified ||
                    preconditionState == PreconditionState.ShouldProcess))
                {
                    return SetRangeHeaders(context, httpRequestHeaders, fileLength, lastModified, etag);
                }
                if (preconditionState == PreconditionState.NotModified)
                {
                    serveBody = false;
                    response.StatusCode = StatusCodes.Status304NotModified;
                }
                else if (preconditionState == PreconditionState.PreconditionFailed)
                {
                    serveBody = false;
                    response.StatusCode = StatusCodes.Status412PreconditionFailed;
                }
            }

            return (range: null, rangeLength: 0, serveBody: serveBody);
        }

        private static void SetContentType(ActionContext context, FileResult result)
        {
            var response = context.HttpContext.Response;
            response.ContentType = result.ContentType;
        }

        private static void SetContentDispositionHeader(ActionContext context, FileResult result)
        {
            if (!string.IsNullOrEmpty(result.FileDownloadName))
            {
                // From RFC 2183, Sec. 2.3:
                // The sender may want to suggest a filename to be used if the entity is
                // detached and stored in a separate file. If the receiving MUA writes
                // the entity to a file, the suggested filename should be used as a
                // basis for the actual filename, where possible.
                var contentDisposition = new ContentDispositionHeaderValue("attachment");
                contentDisposition.SetHttpFileName(result.FileDownloadName);
                context.HttpContext.Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
            }
        }

        private static void SetAcceptRangeHeader(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.Headers[HeaderNames.AcceptRanges] = AcceptRangeHeaderValue;
        }

        private static PreconditionState GetEtagMatchState(
            IList<EntityTagHeaderValue> etagHeader,
            EntityTagHeaderValue etag,
            PreconditionState matchFoundState,
            PreconditionState matchNotFoundState)
        {
            if (etagHeader != null && etagHeader.Any())
            {
                var state = matchNotFoundState;
                foreach (var entityTag in etagHeader)
                {
                    if (entityTag.Equals(EntityTagHeaderValue.Any) || entityTag.Compare(etag, useStrongComparison: true))
                    {
                        state = matchFoundState;
                        break;
                    }
                }

                return state;
            }

            return PreconditionState.Unspecified;
        }

        // Internal for testing
        internal static PreconditionState GetPreconditionState(
            ActionContext context,
            RequestHeaders httpRequestHeaders,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue etag = null)
        {
            var ifMatchState = PreconditionState.Unspecified;
            var ifNoneMatchState = PreconditionState.Unspecified;
            var ifModifiedSinceState = PreconditionState.Unspecified;
            var ifUnmodifiedSinceState = PreconditionState.Unspecified;
            var ifRangeState = PreconditionState.Unspecified;

            // 14.24 If-Match
            var ifMatch = httpRequestHeaders.IfMatch;
            if (etag != null)
            {
                ifMatchState = GetEtagMatchState(
                    etagHeader: ifMatch,
                    etag: etag,
                    matchFoundState: PreconditionState.ShouldProcess,
                    matchNotFoundState: PreconditionState.PreconditionFailed);
            }

            // 14.26 If-None-Match
            var ifNoneMatch = httpRequestHeaders.IfNoneMatch;
            if (etag != null)
            {
                ifNoneMatchState = GetEtagMatchState(
                    etagHeader: ifNoneMatch,
                    etag: etag,
                    matchFoundState: PreconditionState.NotModified,
                    matchNotFoundState: PreconditionState.ShouldProcess);
            }

            var now = DateTimeOffset.UtcNow;

            // 14.25 If-Modified-Since
            var ifModifiedSince = httpRequestHeaders.IfModifiedSince;
            if (lastModified.HasValue && ifModifiedSince.HasValue && ifModifiedSince <= now)
            {
                var modified = ifModifiedSince < lastModified;
                ifModifiedSinceState = modified ? PreconditionState.ShouldProcess : PreconditionState.NotModified;
            }

            // 14.28 If-Unmodified-Since
            var ifUnmodifiedSince = httpRequestHeaders.IfUnmodifiedSince;
            if (lastModified.HasValue && ifUnmodifiedSince.HasValue && ifUnmodifiedSince <= now)
            {
                var unmodified = ifUnmodifiedSince >= lastModified;
                ifUnmodifiedSinceState = unmodified ? PreconditionState.ShouldProcess : PreconditionState.PreconditionFailed;
            }

            var ifRange = httpRequestHeaders.IfRange;
            if (ifRange != null)
            {
                // If the validator given in the If-Range header field matches the
                // current validator for the selected representation of the target
                // resource, then the server SHOULD process the Range header field as
                // requested.  If the validator does not match, the server MUST ignore
                // the Range header field.
                if (ifRange.LastModified.HasValue)
                {
                    if (lastModified.HasValue && lastModified > ifRange.LastModified)
                    {
                        ifRangeState = PreconditionState.IgnoreRangeRequest;
                    }
                }
                else if (etag != null && ifRange.EntityTag != null && !ifRange.EntityTag.Compare(etag, useStrongComparison: true))
                {
                    ifRangeState = PreconditionState.IgnoreRangeRequest;
                }
            }

            var state = GetMaxPreconditionState(ifMatchState, ifNoneMatchState, ifModifiedSinceState, ifUnmodifiedSinceState, ifRangeState);
            return state;
        }

        private static PreconditionState GetMaxPreconditionState(params PreconditionState[] states)
        {
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

        private static (RangeItemHeaderValue range, long rangeLength, bool serveBody) SetRangeHeaders(
            ActionContext context,
            RequestHeaders httpRequestHeaders,
            long? fileLength,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue etag = null)
        {
            var response = context.HttpContext.Response;
            var httpResponseHeaders = response.GetTypedHeaders();

            // Checked for presence of Range header explicitly before calling this method.
            // Range may be null for parsing errors, multiple ranges and when the file length is missing.
            var range = fileLength.HasValue ? ParseRange(context, httpRequestHeaders, fileLength.Value, lastModified, etag) : null;
            if (range == null)
            {
                // 14.16 Content-Range - A server sending a response with status code 416 (Requested range not satisfiable)
                // SHOULD include a Content-Range field with a byte-range-resp-spec of "*". The instance-length specifies
                // the current length of the selected resource.  e.g. */length
                response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                if (fileLength.HasValue)
                {
                    httpResponseHeaders.ContentRange = new ContentRangeHeaderValue(fileLength.Value);
                }

                return (range: null, rangeLength: 0, serveBody: false);
            }

            httpResponseHeaders.ContentRange = new ContentRangeHeaderValue(
                range.From.Value,
                range.To.Value,
                fileLength.Value);

            response.StatusCode = StatusCodes.Status206PartialContent;
            var rangeLength = SetContentLength(context, range);
            return (range, rangeLength, serveBody: true);
        }

        private static long SetContentLength(ActionContext context, RangeItemHeaderValue range)
        {
            var start = range.From.Value;
            var end = range.To.Value;
            var length = end - start + 1;
            var response = context.HttpContext.Response;
            response.ContentLength = length;
            return length;
        }

        private static RangeItemHeaderValue ParseRange(
            ActionContext context,
            RequestHeaders httpRequestHeaders,
            long fileLength,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue etag = null)
        {
            var httpContext = context.HttpContext;
            var response = httpContext.Response;

            var range = RangeHelper.ParseRange(httpContext, httpRequestHeaders, lastModified, etag);

            if (range != null)
            {
                var normalizedRanges = RangeHelper.NormalizeRanges(range, fileLength);
                if (normalizedRanges == null || normalizedRanges.Count == 0)
                {
                    return null;
                }

                return normalizedRanges.Single();
            }

            return null;
        }

        protected static ILogger CreateLogger<T>(ILoggerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.CreateLogger<T>();
        }

        protected static async Task WriteFileAsync(HttpContext context, Stream fileStream, RangeItemHeaderValue range, long rangeLength)
        {
            var outputStream = context.Response.Body;
            using (fileStream)
            {
                try
                {
                    if (range == null)
                    {
                        await StreamCopyOperation.CopyToAsync(fileStream, outputStream, count: null, bufferSize: BufferSize, cancel: context.RequestAborted);
                    }
                    else
                    {
                        fileStream.Seek(range.From.Value, SeekOrigin.Begin);
                        await StreamCopyOperation.CopyToAsync(fileStream, outputStream, rangeLength, BufferSize, context.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Don't throw this exception, it's most likely caused by the client disconnecting.
                    // However, if it was cancelled for any other reason we need to prevent empty responses.
                    context.Abort();
                }
            }
        }
    }
}
