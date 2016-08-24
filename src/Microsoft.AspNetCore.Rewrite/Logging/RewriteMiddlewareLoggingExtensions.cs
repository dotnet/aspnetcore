// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Rewrite.Logging
{
    internal static class RewriteMiddlewareLoggingExtensions
    {
        private static readonly Action<ILogger, Exception> _requestContinueResults;
        private static readonly Action<ILogger, string, int, Exception> _requestResponseComplete;
        private static readonly Action<ILogger, Exception> _requestStopRules;
        private static readonly Action<ILogger, string, Exception> _urlRewriteDidNotMatchRule;
        private static readonly Action<ILogger, string, Exception> _urlRewriteMatchedRule;
        private static readonly Action<ILogger, Exception> _modRewriteDidNotMatchRule;
        private static readonly Action<ILogger, Exception> _modRewriteMatchedRule;

        static RewriteMiddlewareLoggingExtensions()
        {
            _requestContinueResults = LoggerMessage.Define(
                            LogLevel.Debug,
                            1,
                            "Request is continuing in applying rules.");
            _requestResponseComplete = LoggerMessage.Define<string, int>(
                            LogLevel.Debug,
                            2,
                            "Request is done processing, Location header '{Location}' with status code '{StatusCode}'.");
            _requestStopRules = LoggerMessage.Define(
                            LogLevel.Debug,
                            3,
                            "Request is done applying rules.");
            _urlRewriteDidNotMatchRule = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            4,
                            "Request did not match current rule '{Name}'.");

            _urlRewriteMatchedRule = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            5,
                            "Request matched current UrlRewriteRule '{Name}'.");

            _modRewriteDidNotMatchRule = LoggerMessage.Define(
                            LogLevel.Debug,
                            6,
                            "Request matched current ModRewriteRule.");

            _modRewriteMatchedRule = LoggerMessage.Define(
                            LogLevel.Debug,
                            7,
                            "Request matched current ModRewriteRule.");
        }

        public static void RewriteMiddlewareRequestContinueResults(this ILogger logger)
        {
            _requestContinueResults(logger, null);
        }

        public static void RewriteMiddlewareRequestResponseComplete(this ILogger logger, string location, int statusCode)
        {
            _requestResponseComplete(logger, location, statusCode, null);
        }

        public static void RewriteMiddlewareRequestStopRules(this ILogger logger)
        {
            _requestStopRules(logger, null);
        }

        public static void UrlRewriteDidNotMatchRule(this ILogger logger, string name)
        {
            _urlRewriteDidNotMatchRule(logger, name, null);
        }

        public static void UrlRewriteMatchedRule(this ILogger logger, string name)
        {
            _urlRewriteMatchedRule(logger, name, null);
        }

        public static void ModRewriteDidNotMatchRule(this ILogger logger)
        {
            _modRewriteDidNotMatchRule(logger, null);
        }
        public static void ModRewriteMatchedRule(this ILogger logger)
        {
            _modRewriteMatchedRule(logger, null);
        }
    }
}
