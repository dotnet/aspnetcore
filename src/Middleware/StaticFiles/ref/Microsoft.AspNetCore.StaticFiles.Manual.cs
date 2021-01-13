// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Internal
{
    internal static partial class RangeHelper
    {
        internal static Microsoft.Net.Http.Headers.RangeItemHeaderValue NormalizeRange(Microsoft.Net.Http.Headers.RangeItemHeaderValue range, long length) { throw null; }
        public static (bool isRangeRequest, Microsoft.Net.Http.Headers.RangeItemHeaderValue range) ParseRange(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.Headers.RequestHeaders requestHeaders, long length, Microsoft.Extensions.Logging.ILogger logger) { throw null; }
    }
}

namespace Microsoft.AspNetCore.StaticFiles
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct StaticFileContext
    {
        private readonly Microsoft.AspNetCore.Http.HttpContext _context;
        private readonly Microsoft.AspNetCore.Builder.StaticFileOptions _options;
        private readonly Microsoft.AspNetCore.Http.HttpRequest _request;
        private readonly Microsoft.AspNetCore.Http.HttpResponse _response;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly Microsoft.Extensions.FileProviders.IFileProvider _fileProvider;
        private readonly string _method;
        private readonly string _contentType;
        private Microsoft.Extensions.FileProviders.IFileInfo _fileInfo;
        private Microsoft.Net.Http.Headers.EntityTagHeaderValue _etag;
        private Microsoft.AspNetCore.Http.Headers.RequestHeaders _requestHeaders;
        private Microsoft.AspNetCore.Http.Headers.ResponseHeaders _responseHeaders;
        private Microsoft.Net.Http.Headers.RangeItemHeaderValue _range;
        private long _length;
        private readonly Microsoft.AspNetCore.Http.PathString _subPath;
        private System.DateTimeOffset _lastModified;
        private Microsoft.AspNetCore.StaticFiles.StaticFileContext.PreconditionState _ifMatchState;
        private Microsoft.AspNetCore.StaticFiles.StaticFileContext.PreconditionState _ifNoneMatchState;
        private Microsoft.AspNetCore.StaticFiles.StaticFileContext.PreconditionState _ifModifiedSinceState;
        private Microsoft.AspNetCore.StaticFiles.StaticFileContext.PreconditionState _ifUnmodifiedSinceState;
        private Microsoft.AspNetCore.StaticFiles.StaticFileContext.RequestType _requestType;
        public StaticFileContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Builder.StaticFileOptions options, Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.FileProviders.IFileProvider fileProvider, string contentType, Microsoft.AspNetCore.Http.PathString subPath) { throw null; }
        public bool IsGetMethod { get { throw null; } }
        public bool IsHeadMethod { get { throw null; } }
        public bool IsRangeRequest { get { throw null; } }
        public string PhysicalPath { get { throw null; } }
        public string SubPath { get { throw null; } }
        public void ApplyResponseHeaders(int statusCode) { }
        public void ComprehendRequestHeaders() { }
        public Microsoft.AspNetCore.StaticFiles.StaticFileContext.PreconditionState GetPreconditionState() { throw null; }
        public bool LookupFileInfo() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task SendAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task SendRangeAsync() { throw null; }
        public System.Threading.Tasks.Task SendStatusAsync(int statusCode) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ServeStaticFile(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.RequestDelegate next) { throw null; }
        internal enum PreconditionState : byte
        {
            Unspecified = (byte)0,
            NotModified = (byte)1,
            ShouldProcess = (byte)2,
            PreconditionFailed = (byte)3,
        }
        [System.FlagsAttribute]
        private enum RequestType : byte
        {
            Unspecified = (byte)0,
            IsHead = (byte)1,
            IsGet = (byte)2,
            IsRange = (byte)4,
        }
    }
    public partial class StaticFileMiddleware
    {
        internal static bool LookupContentType(Microsoft.AspNetCore.StaticFiles.IContentTypeProvider contentTypeProvider, Microsoft.AspNetCore.Builder.StaticFileOptions options, Microsoft.AspNetCore.Http.PathString subPath, out string contentType) { throw null; }
        internal static bool ValidatePath(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.PathString matchUrl, out Microsoft.AspNetCore.Http.PathString subPath) { throw null; }
    }
}
