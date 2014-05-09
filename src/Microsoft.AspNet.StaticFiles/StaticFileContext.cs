// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.StaticFiles.Infrastructure;

namespace Microsoft.AspNet.StaticFiles
{
    internal struct StaticFileContext
    {
        private readonly HttpContext _context;
        private readonly StaticFileOptions _options;
        private readonly PathString _matchUrl;
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;
        private string _method;
        private bool _isGet;
        private bool _isHead;
        private PathString _subPath;
        private string _contentType;
        private IFileInfo _fileInfo;
        private long _length;
        private DateTime _lastModified;
        private string _lastModifiedString;
        private string _etag;
        private string _etagQuoted;

        private PreconditionState _ifMatchState;
        private PreconditionState _ifNoneMatchState;
        private PreconditionState _ifModifiedSinceState;
        private PreconditionState _ifUnmodifiedSinceState;

        private IList<Tuple<long, long>> _ranges;

        public StaticFileContext(HttpContext context, StaticFileOptions options, PathString matchUrl)
        {
            _context = context;
            _options = options;
            _matchUrl = matchUrl;
            _request = context.Request;
            _response = context.Response;

            _method = null;
            _isGet = false;
            _isHead = false;
            _subPath = PathString.Empty;
            _contentType = null;
            _fileInfo = null;
            _length = 0;
            _lastModified = new DateTime();
            _etag = null;
            _etagQuoted = null;
            _lastModifiedString = null;
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
            bool found = _options.FileSystem.TryGetFileInfo(_subPath.Value, out _fileInfo);
            if (found)
            {
                _length = _fileInfo.Length;

                DateTime last = _fileInfo.LastModified;
                // Truncate to the second.
                _lastModified = new DateTime(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Kind);
                _lastModifiedString = _lastModified.ToString(Constants.HttpDateFormat, CultureInfo.InvariantCulture);

                long etagHash = _lastModified.ToFileTimeUtc() ^ _length;
                _etag = Convert.ToString(etagHash, 16);
                _etagQuoted = '\"' + _etag + '\"';
            }
            return found;
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
            IList<string> ifMatch = _request.Headers.GetCommaSeparatedValues(Constants.IfMatch); // Removes quotes
            if (ifMatch != null)
            {
                _ifMatchState = PreconditionState.PreconditionFailed;
                foreach (var segment in ifMatch)
                {
                    if (segment.Equals("*", StringComparison.Ordinal)
                        || segment.Equals(_etag, StringComparison.Ordinal))
                    {
                        _ifMatchState = PreconditionState.ShouldProcess;
                        break;
                    }
                }
            }

            // 14.26 If-None-Match
            IList<string> ifNoneMatch = _request.Headers.GetCommaSeparatedValues(Constants.IfNoneMatch);
            if (ifNoneMatch != null)
            {
                _ifNoneMatchState = PreconditionState.ShouldProcess;
                foreach (var segment in ifNoneMatch)
                {
                    if (segment.Equals("*", StringComparison.Ordinal)
                        || segment.Equals(_etag, StringComparison.Ordinal))
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
            string ifModifiedSinceString = _request.Headers.Get(Constants.IfModifiedSince);
            DateTime ifModifiedSince;
            if (Helpers.TryParseHttpDate(ifModifiedSinceString, out ifModifiedSince))
            {
                bool modified = ifModifiedSince < _lastModified;
                _ifModifiedSinceState = modified ? PreconditionState.ShouldProcess : PreconditionState.NotModified;
            }

            // 14.28 If-Unmodified-Since
            string ifUnmodifiedSinceString = _request.Headers.Get(Constants.IfUnmodifiedSince);
            DateTime ifUnmodifiedSince;
            if (Helpers.TryParseHttpDate(ifUnmodifiedSinceString, out ifUnmodifiedSince))
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

            string rangeHeader = _request.Headers.Get(Constants.Range);
            IList<Tuple<long?, long?>> ranges;
            if (!RangeHelpers.TryParseRanges(rangeHeader, out ranges))
            {
                return;
            }

            if (ranges.Count > 1)
            {
                // multiple range headers not yet supported
                return;
            }

            // 14.27 If-Range
            string ifRangeHeader = _request.Headers.Get(Constants.IfRange);
            if (!string.IsNullOrWhiteSpace(ifRangeHeader))
            {
                // If the validator given in the If-Range header field matches the
                // current validator for the selected representation of the target
                // resource, then the server SHOULD process the Range header field as
                // requested.  If the validator does not match, the server MUST ignore
                // the Range header field.
                DateTime ifRangeLastModified;
                bool ignoreRangeHeader = false;
                if (Helpers.TryParseHttpDate(ifRangeHeader, out ifRangeLastModified))
                {
                    if (_lastModified > ifRangeLastModified)
                    {
                        ignoreRangeHeader = true;
                    }
                }
                else
                {
                    if (!_etagQuoted.Equals(ifRangeHeader))
                    {
                        ignoreRangeHeader = true;
                    }
                }
                if (ignoreRangeHeader)
                {
                    return;
                }
            }

            _ranges = RangeHelpers.NormalizeRanges(ranges, _length);
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
                _response.Headers.Set(Constants.LastModified, _lastModifiedString);
                _response.Headers.Set(Constants.ETag, _etagQuoted);
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

            return Constants.CompletedTask;
        }

        public async Task SendAsync()
        {
            ApplyResponseHeaders(Constants.Status200Ok);

            string physicalPath = _fileInfo.PhysicalPath;
            var sendFile = _context.GetFeature<IHttpSendFileFeature>();
            if (sendFile != null && !string.IsNullOrEmpty(physicalPath))
            {
                await sendFile.SendFileAsync(physicalPath, 0, _length, _request.CallCanceled);
                return;
            }

            Stream readStream = _fileInfo.CreateReadStream();
            try
            {
                await StreamCopyOperation.CopyToAsync(readStream, _response.Body, _length, _request.CallCanceled);
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
                _response.Headers[Constants.ContentRange] = "bytes */" + _length.ToString(CultureInfo.InvariantCulture);
                ApplyResponseHeaders(Constants.Status416RangeNotSatisfiable);
                return;
            }

            // Multi-range is not supported.
            Debug.Assert(_ranges.Count == 1);

            long start, length;
            _response.Headers[Constants.ContentRange] = ComputeContentRange(_ranges[0], out start, out length);
            _response.ContentLength = length;
            ApplyResponseHeaders(Constants.Status206PartialContent);

            string physicalPath = _fileInfo.PhysicalPath;
            var sendFile = _context.GetFeature<IHttpSendFileFeature>();
            if (sendFile != null && !string.IsNullOrEmpty(physicalPath))
            {
                await sendFile.SendFileAsync(physicalPath, start, length, _request.CallCanceled);
                return;
            }

            Stream readStream = _fileInfo.CreateReadStream();
            try
            {
                readStream.Seek(start, SeekOrigin.Begin); // TODO: What if !CanSeek?
                await StreamCopyOperation.CopyToAsync(readStream, _response.Body, length, _request.CallCanceled);
            }
            finally
            {
                readStream.Dispose();
            }
        }

        // Note: This assumes ranges have been normalized to absolute byte offsets.
        private string ComputeContentRange(Tuple<long, long> range, out long start, out long length)
        {
            start = range.Item1;
            long end = range.Item2;
            length = end - start + 1;
            return string.Format(CultureInfo.InvariantCulture, "bytes {0}-{1}/{2}", start, end, _length);
        }
    }
}
