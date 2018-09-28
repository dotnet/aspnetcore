// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.ResponseCompression.Internal
{
    internal static class ResponseCompressionLoggingExtensions
    {
        private static readonly Action<ILogger, Exception> _noAcceptEncoding;
        private static readonly Action<ILogger, Exception> _noCompressionForHttps;
        private static readonly Action<ILogger, Exception> _requestAcceptsCompression;
        private static readonly Action<ILogger, string, Exception> _noCompressionDueToHeader;
        private static readonly Action<ILogger, string, Exception> _noCompressionForContentType;
        private static readonly Action<ILogger, Exception> _shouldCompressResponse;
        private static readonly Action<ILogger, Exception> _noCompressionProvider;
        private static readonly Action<ILogger, string, Exception> _compressWith;

        static ResponseCompressionLoggingExtensions()
        {
            _noAcceptEncoding = LoggerMessage.Define(LogLevel.Debug, 1, "No response compression available, the Accept-Encoding header is missing or invalid.");
            _noCompressionForHttps = LoggerMessage.Define(LogLevel.Debug, 2, "No response compression available for HTTPS requests. See ResponseCompressionOptions.EnableForHttps.");
            _requestAcceptsCompression = LoggerMessage.Define(LogLevel.Trace, 3, "This request accepts compression.");
            _noCompressionDueToHeader = LoggerMessage.Define<string>(LogLevel.Debug, 4, "Response compression disabled due to the {header} header.");
            _noCompressionForContentType = LoggerMessage.Define<string>(LogLevel.Debug, 5, "Response compression is not enabled for the Content-Type '{header}'.");
            _shouldCompressResponse = LoggerMessage.Define(LogLevel.Trace, 6, "Response compression is available for this Content-Type.");
            _noCompressionProvider = LoggerMessage.Define(LogLevel.Debug, 7, "No matching response compression provider found.");
            _compressWith = LoggerMessage.Define<string>(LogLevel.Debug, 8, "The response will be compressed with '{provider}'.");
        }

        public static void NoAcceptEncoding(this ILogger logger)
        {
            _noAcceptEncoding(logger, null);
        }

        public static void NoCompressionForHttps(this ILogger logger)
        {
            _noCompressionForHttps(logger, null);
        }

        public static void RequestAcceptsCompression(this ILogger logger)
        {
            _requestAcceptsCompression(logger, null);
        }

        public static void NoCompressionDueToHeader(this ILogger logger, string header)
        {
            _noCompressionDueToHeader(logger, header, null);
        }

        public static void NoCompressionForContentType(this ILogger logger, string header)
        {
            _noCompressionForContentType(logger, header, null);
        }

        public static void ShouldCompressResponse(this ILogger logger)
        {
            _shouldCompressResponse(logger, null);
        }

        public static void NoCompressionProvider(this ILogger logger)
        {
            _noCompressionProvider(logger, null);
        }

        public static void CompressingWith(this ILogger logger, string provider)
        {
            _compressWith(logger, provider, null);
        }
    }
}
