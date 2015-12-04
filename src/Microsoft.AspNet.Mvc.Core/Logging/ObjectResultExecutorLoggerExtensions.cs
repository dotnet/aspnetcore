// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class ObjectResultExecutorLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _objectResultExecuting;
        private static readonly Action<ILogger, string, Exception> _noFormatter;
        private static readonly Action<ILogger, IOutputFormatter, string, Exception> _formatterSelected;
        private static readonly Action<ILogger, string, Exception> _skippedContentNegotiation;
        private static readonly Action<ILogger, string, Exception> _noAcceptForNegotiation;
        private static readonly Action<ILogger, IEnumerable<MediaTypeHeaderValue>, Exception> _noFormatterFromNegotiation;

        static ObjectResultExecutorLoggerExtensions()
        {
            _noFormatter = LoggerMessage.Define<string>(
                LogLevel.Warning,
                1,
                "No output formatter was found for content type '{ContentType}' to write the response.");
            _objectResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing ObjectResult, writing value {Value}.");
            _formatterSelected = LoggerMessage.Define<IOutputFormatter, string>(
                LogLevel.Debug,
                2,
                "Selected output formatter '{OutputFormatter}' and content type '{ContentType}' to write the response.");
            _skippedContentNegotiation = LoggerMessage.Define<string>(
                LogLevel.Debug,
                3,
                "Skipped content negotiation as content type '{ContentType}' is explicitly set for the response.");
            _noAcceptForNegotiation = LoggerMessage.Define<string>(
                LogLevel.Debug,
                4,
                "No information found on request to perform content negotiation.");
            _noFormatterFromNegotiation = LoggerMessage.Define<IEnumerable<MediaTypeHeaderValue>>(
                LogLevel.Debug,
                5,
                "Could not find an output formatter based on content negotiation. Accepted types were ({AcceptTypes})");
        }

        public static void ObjectResultExecuting(this ILogger logger, object value)
        {
            _objectResultExecuting(logger, Convert.ToString(value), null);
        }

        public static void NoFormatter(
            this ILogger logger,
            OutputFormatterWriteContext formatterContext)
        {
            _noFormatter(logger, Convert.ToString(formatterContext.ContentType), null);
        }

        public static void FormatterSelected(
            this ILogger logger,
            IOutputFormatter outputFormatter,
            OutputFormatterWriteContext context)
        {
            var contentType = Convert.ToString(context.ContentType);
            _formatterSelected(logger, outputFormatter, contentType, null);
        }

        public static void SkippedContentNegotiation(this ILogger logger, MediaTypeHeaderValue contentType)
        {
            _skippedContentNegotiation(logger, Convert.ToString(contentType), null);
        }

        public static void NoAcceptForNegotiation(this ILogger logger)
        {
            _noAcceptForNegotiation(logger, null, null);
        }

        public static void NoFormatterFromNegotiation(this ILogger logger, IEnumerable<MediaTypeHeaderValue> acceptTypes)
        {
            _noFormatterFromNegotiation(logger, acceptTypes, null);
        }
    }
}
