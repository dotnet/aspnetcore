// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Headers;
using Microsoft.AspNet.StaticFiles.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.StaticFiles
{
    internal struct StaticFileContext
    {
        private readonly HttpContext _context;
        private readonly StaticFileOptions _options;
        private readonly PathString _matchUrl;
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;
        private readonly ILogger _logger;
        private string _method;
        private bool _isGet;
        private bool _isHead;
        private PathString _subPath;
        private string _contentType;
        private IFileInfo _fileInfo;
        private long _length;
        private DateTimeOffset _lastModified;
        private EntityTagHeaderValue _etag;

        private RequestHeaders _requestHeaders;
        private ResponseHeaders _responseHeaders;

        private PreconditionState _ifMatchState;
        private PreconditionState _ifNoneMatchState;
        private PreconditionState _ifModifiedSinceState;
        private PreconditionState _ifUnmodifiedSinceState;

        private IList<RangeItemHeaderValue> _ranges;

        public StaticFileContext(HttpContext context, StaticFileOptions options, PathString matchUrl, ILogger logger)
        {
            _context = context;
            _options = options;
            _matchUrl = matchUrl;
            _request = context.Request;
            _response = context.Response;
            _logger = logger;
            _requestHeaders = _request.GetTypedHeaders();
            _responseHeaders = _response.GetTypedHeaders();

            _method = null;
            _isGet = false;
            _isHead = false;
            _subPath = PathString.Empty;
            _contentType = null;
            _fileInfo = null;
            _length = 0;
            _lastModified = new DateTimeOffset();
            _etag = null;
            _ifMatchState = PreconditionState.Unspecified;
            _ifNoneMatchState = PreconditionState.Unspecified;
            _ifModifiedSinceState = PreconditionState.Unspecified;
            _ifUnmodifiedSinceState = PreconditionState.Unspecified;
            _ranges = null;
        }

        internal enum PreconditionState
        {
            Unspecified,
            NotModified,
            ShouldProcess,
            PreconditionFailed,
        }

        public bool IsHeadMethod
        {
            get { return _isHead; }
        }

        public bool IsRangeRequest
        {
            get { return _ranges != null; }
        }

        public string SubPath
        {
            get { return _subPath.Value; }
        }

        public string PhysicalPath
        {
            get { return _fileInfo?.PhysicalPath; }
        }

        public bool ValidateMethod()
        {
            _method = _request.Method;
            _isGet = Helpers.IsGetMethod(_method);
            _isHead = Helpers.IsHeadMethod(_method);
            return _isGet || _isHead;
        }

        // Check if the URL matches any expected paths
        public bool ValidatePath()
        {
            return Helpers.TryMatchPath(_context, _matchUrl, forDirectory: false, subpath: out _subPath);
        }

        public bool LookupContentType()
        {
            if (_options.ContentTypeProvider.TryGetContentType(_subPath.Value, out _contentType))
            {
                return true;
            }

            if (_options.ServeUnknownFileTypes)
            {
                _contentType = _options.DefaultContentType;
                return true;
            }

            return false;
        }

        public bool LookupFileInfo()
        {
            _fileInfo = _options.FileProvider.GetFileInfo(_subPath.Value);
            if (_fileInfo.Exists)
            {
                _length = _fileInfo.Length;

                DateTimeOffset last = _fileInfo.LastModified;
                // Truncate to the second.
                _lastModified = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset);

                long etagHash = _lastModified.ToFileTime() ^ _length;
                _etag = new EntityTagHeaderValue('\"' + Convert.ToString(etagHash, 16) + '\"');
            }
            return _fileInfo.Exists;
        }

        public void ComprehendRequestHeaders()
        {
            ComputeIfMatch();

            ComputeIfModifiedSince();

            ComputeRange();
        }

        private void ComputeIfMatch()
        {
            // 14.24 If-Match
            var ifMatch = _requestHeaders.IfMatch;
            if (ifMatch != null && ifMatch.Any())
            {
                _ifMatchState = PreconditionState.PreconditionFailed;
                foreach (var etag in ifMatch)
                {
                    if (etag.Equals(EntityTagHeaderValue.Any) || etag.Equals(_etag))
                    {
                        _ifMatchState = PreconditionState.ShouldProcess;
                        break;
                    }
                }
            }

            // 14.26 If-None-Match
            var ifNoneMatch = _requestHeaders.IfNoneMatch;
            if (ifNoneMatch != null && ifNoneMatch.Any())
            {
                _ifNoneMatchState = PreconditionState.ShouldProcess;
                foreach (var etag in ifNoneMatch)
                {
                    if (etag.Equals(EntityTagHeaderValue.Any) || etag.Equals(_etag))
                    {
                        _ifNoneMatchState = PreconditionState.NotModified;
                        break;
                    }
                }
            }
        }

        private void ComputeIfModifiedSince()
        {
            // 14.25 If-Modified-Since
            var ifModifiedSince = _requestHeaders.IfModifiedSince;
            if (ifModifiedSince.HasValue)
            {
                bool modified = ifModifiedSince < _lastModified;
                _ifModifiedSinceState = modified ? PreconditionState.ShouldProcess : PreconditionState.NotModified;
            }

            // 14.28 If-Unmodified-Since
            var ifUnmodifiedSince = _requestHeaders.IfUnmodifiedSince;
            if (ifUnmodifiedSince.HasValue)
            {
                bool unmodified = ifUnmodifiedSince >= _lastModified;
                _ifUnmodifiedSinceState = unmodified ? PreconditionState.ShouldProcess : PreconditionState.PreconditionFailed;
            }
        }

        private void ComputeRange()
        {
            // 14.35 Range
            // http://tools.ietf.org/html/draft-ietf-httpbis-p5-range-24

            // A server MUST ignore a Range header field received with a request method other
            // than GET.
            if (!_isGet)
            {
                return;
            }

            var rangeHeader = _requestHeaders.Range;
            if (rangeHeader == null)
            {
                return;
            }

            if (rangeHeader.Ranges.Count > 1)
            {
                // The spec allows for multiple ranges but we choose not to support them because the client may request
                // very strange ranges (e.g. each byte separately, overlapping ranges, etc.) that could negatively
                // impact the server. Ignore the header and serve the response normally.
                _logger.LogMultipleFileRanges(rangeHeader.ToString());
                return;
            }

            // 14.27 If-Range
            var ifRangeHeader = _requestHeaders.IfRange;
            if (ifRangeHeader != null)
            {
                // If the validator given in the If-Range header field matches the
                // current validator for the selected representation of the target
                // resource, then the server SHOULD process the Range header field as
                // requested.  If the validator does not match, the server MUST ignore
                // the Range header field.
                bool ignoreRangeHeader = false;
                if (ifRangeHeader.LastModified.HasValue)
                {
                    if (_lastModified > ifRangeHeader.LastModified)
                    {
                        ignoreRangeHeader = true;
                    }
                }
                else if (ifRangeHeader.EntityTag != null && !_etag.Equals(ifRangeHeader.EntityTag))
                {
                    ignoreRangeHeader = true;
                }
                if (ignoreRangeHeader)
                {
                    return;
                }
            }

            _ranges = RangeHelpers.NormalizeRanges(rangeHeader.Ranges, _length);
        }

        public void ApplyResponseHeaders(int statusCode)
        {
            _response.StatusCode = statusCode;
            if (statusCode < 400)
            {
                // these headers are returned for 200, 206, and 304
                // they are not returned for 412 and 416
                if (!string.IsNullOrEmpty(_contentType))
                {
                    _response.ContentType = _contentType;
                }
                _responseHeaders.LastModified = _lastModified;
                _responseHeaders.ETag = _etag;
                _responseHeaders.Headers[HeaderNames.AcceptRanges] = "bytes";
            }
            if (statusCode == Constants.Status200Ok)
            {
                // this header is only returned here for 200
                // it already set to the returned range for 206
                // it is not returned for 304, 412, and 416
                _response.ContentLength = _length;
            }
            _options.OnPrepareResponse(new StaticFileResponseContext()
            {
                Context = _context,
                File = _fileInfo,
            });
        }

        public PreconditionState GetPreconditionState()
        {
            return GetMaxPreconditionState(_ifMatchState, _ifNoneMatchState,
                _ifModifiedSinceState, _ifUnmodifiedSinceState);
        }

        private static PreconditionState GetMaxPreconditionState(params PreconditionState[] states)
        {
            PreconditionState max = PreconditionState.Unspecified;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] > max)
                {
                    max = states[i];
                }
            }
            return max;
        }

        public Task SendStatusAsync(int statusCode)
        {
            ApplyResponseHeaders(statusCode);

            _logger.LogHandled(statusCode, SubPath);
            return Constants.CompletedTask;
        }

        public async Task SendAsync()
        {
            ApplyResponseHeaders(Constants.Status200Ok);

            string physicalPath = _fileInfo.PhysicalPath;
            var sendFile = _context.Features.Get<IHttpSendFileFeature>();
            if (sendFile != null && !string.IsNullOrEmpty(physicalPath))
            {
                await sendFile.SendFileAsync(physicalPath, 0, _length, _context.RequestAborted);
                return;
            }

            Stream readStream = _fileInfo.CreateReadStream();
            try
            {
                await StreamCopyOperation.CopyToAsync(readStream, _response.Body, _length, _context.RequestAborted);
            }
            finally
            {
                readStream.Dispose();
            }
        }

        // When there is only a single range the bytes are sent directly in the body.
        internal async Task SendRangeAsync()
        {
            bool rangeNotSatisfiable = false;
            if (_ranges.Count == 0)
            {
                rangeNotSatisfiable = true;
            }

            if (rangeNotSatisfiable)
            {
                // 14.16 Content-Range - A server sending a response with status code 416 (Requested range not satisfiable)
                // SHOULD include a Content-Range field with a byte-range-resp-spec of "*". The instance-length specifies
                // the current length of the selected resource.  e.g. */length
                _responseHeaders.ContentRange = new ContentRangeHeaderValue(_length);
                ApplyResponseHeaders(Constants.Status416RangeNotSatisfiable);

                _logger.LogRangeNotSatisfiable(SubPath);
                return;
            }

            // Multi-range is not supported.
            Debug.Assert(_ranges.Count == 1);

            long start, length;
            _responseHeaders.ContentRange = ComputeContentRange(_ranges[0], out start, out length);
            _response.ContentLength = length;
            ApplyResponseHeaders(Constants.Status206PartialContent);

            string physicalPath = _fileInfo.PhysicalPath;
            var sendFile = _context.Features.Get<IHttpSendFileFeature>();
            if (sendFile != null && !string.IsNullOrEmpty(physicalPath))
            {
                _logger.LogSendingFileRange(_response.Headers[HeaderNames.ContentRange], physicalPath);
                await sendFile.SendFileAsync(physicalPath, start, length, _context.RequestAborted);
                return;
            }

            Stream readStream = _fileInfo.CreateReadStream();
            try
            {
                readStream.Seek(start, SeekOrigin.Begin); // TODO: What if !CanSeek?
                _logger.LogCopyingFileRange(_response.Headers[HeaderNames.ContentRange], SubPath);
                await StreamCopyOperation.CopyToAsync(readStream, _response.Body, length, _context.RequestAborted);
            }
            finally
            {
                readStream.Dispose();
            }
        }

        // Note: This assumes ranges have been normalized to absolute byte offsets.
        private ContentRangeHeaderValue ComputeContentRange(RangeItemHeaderValue range, out long start, out long length)
        {
            start = range.From.Value;
            long end = range.To.Value;
            length = end - start + 1;
            return new ContentRangeHeaderValue(start, end, _length);
        }
    }
}
