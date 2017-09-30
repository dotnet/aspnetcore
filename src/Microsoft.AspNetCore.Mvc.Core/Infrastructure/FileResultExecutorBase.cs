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
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class FileResultExecutorBase
    {
        private const string AcceptRangeHeaderValue = "bytes";

        protected const int BufferSize = 64 * 1024;

        public FileResultExecutorBase(ILogger logger)
        {
            Logger = logger;
        }

        internal enum PreconditionState
        {
            Unspecified,
            NotModified,
            ShouldProcess,
            PreconditionFailed
        }

        protected ILogger Logger { get; }

        protected virtual (RangeItemHeaderValue range, long rangeLength, bool serveBody) SetHeadersAndLog(
            ActionContext context,
            FileResult result,
            long? fileLength,
            bool enableRangeProcessing,
            DateTimeOffset? lastModified = null,
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

            var request = context.HttpContext.Request;
            var httpRequestHeaders = request.GetTypedHeaders();
            var preconditionState = GetPreconditionState(httpRequestHeaders, lastModified, etag);

            var response = context.HttpContext.Response;
            SetLastModifiedAndEtagHeaders(response, lastModified, etag);

            var serveBody = !HttpMethods.IsHead(request.Method);

            // Short circuit if the preconditional headers process to 304 (NotModified) or 412 (PreconditionFailed)
            if (preconditionState == PreconditionState.NotModified)
            {
                serveBody = false;
                response.StatusCode = StatusCodes.Status304NotModified;
                return (range: null, rangeLength: 0, serveBody);
            }
            else if (preconditionState == PreconditionState.PreconditionFailed)
            {
                serveBody = false;
                response.StatusCode = StatusCodes.Status412PreconditionFailed;
                return (range: null, rangeLength: 0, serveBody);
            }

            if (fileLength.HasValue)
            {
                // Assuming the request is not a range request, and the response body is not empty, the Content-Length header is set to 
                // the length of the entire file. 
                // If the request is a valid range request, this header is overwritten with the length of the range as part of the 
                // range processing (see method SetContentLength).
                if (serveBody)
                {
                    response.ContentLength = fileLength.Value;
                }

                // Handle range request
                if (enableRangeProcessing)
                {
                    SetAcceptRangeHeader(response);

                    // If the request method is HEAD or GET, PreconditionState is Unspecified or ShouldProcess, and IfRange header is valid,
                    // range should be processed and Range headers should be set
                    if ((HttpMethods.IsHead(request.Method) || HttpMethods.IsGet(request.Method))
                        && (preconditionState == PreconditionState.Unspecified || preconditionState == PreconditionState.ShouldProcess)
                        && (IfRangeValid(httpRequestHeaders, lastModified, etag)))
                    {
                        return SetRangeHeaders(context, httpRequestHeaders, fileLength.Value);
                    }
                }
            }

            return (range: null, rangeLength: 0, serveBody);
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

        private static void SetLastModifiedAndEtagHeaders(HttpResponse response, DateTimeOffset? lastModified, EntityTagHeaderValue etag)
        {
            var httpResponseHeaders = response.GetTypedHeaders();
            if (lastModified.HasValue)
            {
                httpResponseHeaders.LastModified = lastModified;
            }
            if (etag != null)
            {
                httpResponseHeaders.ETag = etag;
            }
        }

        private static void SetAcceptRangeHeader(HttpResponse response)
        {
            response.Headers[HeaderNames.AcceptRanges] = AcceptRangeHeaderValue;
        }

        internal static bool IfRangeValid(
            RequestHeaders httpRequestHeaders,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue etag = null)
        {
            // 14.27 If-Range
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
                        return false;
                    }
                }
                else if (etag != null && ifRange.EntityTag != null && !ifRange.EntityTag.Compare(etag, useStrongComparison: true))
                {
                    return false;
                }
            }

            return true;
        }

        // Internal for testing
        internal static PreconditionState GetPreconditionState(
            RequestHeaders httpRequestHeaders,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue etag = null)
        {
            var ifMatchState = PreconditionState.Unspecified;
            var ifNoneMatchState = PreconditionState.Unspecified;
            var ifModifiedSinceState = PreconditionState.Unspecified;
            var ifUnmodifiedSinceState = PreconditionState.Unspecified;

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

            var state = GetMaxPreconditionState(ifMatchState, ifNoneMatchState, ifModifiedSinceState, ifUnmodifiedSinceState);
            return state;
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
            long fileLength)
        {
            var response = context.HttpContext.Response;
            var httpResponseHeaders = response.GetTypedHeaders();

            // Range may be null for empty range header, invalid ranges, parsing errors, multiple ranges 
            // and when the file length is zero.
            var (isRangeRequest, range) = RangeHelper.ParseRange(
                context.HttpContext,
                httpRequestHeaders,
                fileLength);

            if (!isRangeRequest)
            {
                return (range: null, rangeLength: 0, serveBody: true);
            }

            // Requested range is not satisfiable
            if (range == null)
            {
                // 14.16 Content-Range - A server sending a response with status code 416 (Requested range not satisfiable)
                // SHOULD include a Content-Range field with a byte-range-resp-spec of "*". The instance-length specifies
                // the current length of the selected resource.  e.g. */length
                response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                httpResponseHeaders.ContentRange = new ContentRangeHeaderValue(fileLength);

                return (range: null, rangeLength: 0, serveBody: false);
            }

            response.StatusCode = StatusCodes.Status206PartialContent;
            httpResponseHeaders.ContentRange = new ContentRangeHeaderValue(
                range.From.Value,
                range.To.Value,
                fileLength);

            // Overwrite the Content-Length header for valid range requests with the range length.
            var rangeLength = SetContentLength(response, range);

            return (range, rangeLength, serveBody: true);
        }

        private static long SetContentLength(HttpResponse response, RangeItemHeaderValue range)
        {
            var start = range.From.Value;
            var end = range.To.Value;
            var length = end - start + 1;
            response.ContentLength = length;
            return length;
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
