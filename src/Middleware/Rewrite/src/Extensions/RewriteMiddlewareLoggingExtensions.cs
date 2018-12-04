// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Rewrite.Logging
{
    internal static class RewriteMiddlewareLoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _requestContinueResults;
        private static readonly Action<ILogger, string, int, Exception> _requestResponseComplete;
        private static readonly Action<ILogger, string, Exception> _requestStopRules;
        private static readonly Action<ILogger, string, Exception> _urlRewriteDidNotMatchRule;
        private static readonly Action<ILogger, string, Exception> _urlRewriteMatchedRule;
        private static readonly Action<ILogger, Exception> _modRewriteDidNotMatchRule;
        private static readonly Action<ILogger, Exception> _modRewriteMatchedRule;
        private static readonly Action<ILogger, Exception> _redirectedToHttps;
        private static readonly Action<ILogger, Exception> _redirectedToWww;
        private static readonly Action<ILogger, string, Exception> _redirectSummary;
        private static readonly Action<ILogger, string, Exception> _rewriteSummary;
        private static readonly Action<ILogger, string, Exception> _abortedRequest;
        private static readonly Action<ILogger, string, Exception> _customResponse;

        static RewriteMiddlewareLoggingExtensions()
        {
            _requestContinueResults = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            1,
                            "Request is continuing in applying rules. Current url is {currentUrl}");

            _requestResponseComplete = LoggerMessage.Define<string, int>(
                            LogLevel.Debug,
                            2,
                            "Request is done processing. Location header '{Location}' with status code '{StatusCode}'.");

            _requestStopRules = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            3,
                            "Request is done applying rules. Url was rewritten to {rewrittenUrl}");

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

            _redirectedToHttps = LoggerMessage.Define(
                            LogLevel.Information,
                            8,
                            "Request redirected to HTTPS");

            _redirectSummary = LoggerMessage.Define<string>(
                            LogLevel.Information,
                            9,
                            "Request was redirected to {redirectedUrl}");

            _rewriteSummary = LoggerMessage.Define<string>(
                            LogLevel.Information,
                            10,
                            "Request was rewritten to {rewrittenUrl}");

            _abortedRequest = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            11,
                            "Request to {requestedUrl} was aborted");

            _customResponse = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            12,
                            "Request to {requestedUrl} was ended");

            _redirectedToWww = LoggerMessage.Define(
                            LogLevel.Information,
                            13,
                            "Request redirected to www");
        }

        public static void RewriteMiddlewareRequestContinueResults(this ILogger logger, string currentUrl)
        {
            _requestContinueResults(logger, currentUrl, null);
        }

        public static void RewriteMiddlewareRequestResponseComplete(this ILogger logger, string location, int statusCode)
        {
            _requestResponseComplete(logger, location, statusCode, null);
        }

        public static void RewriteMiddlewareRequestStopRules(this ILogger logger, string rewrittenUrl)
        {
            _requestStopRules(logger, rewrittenUrl, null);
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

        public static void RedirectedToHttps(this ILogger logger)
        {
            _redirectedToHttps(logger, null);
        }

        public static void RedirectedToWww(this ILogger logger)
        {
            _redirectedToWww(logger, null);
        }

        public static void RedirectedSummary(this ILogger logger, string redirectedUrl)
        {
            _redirectSummary(logger, redirectedUrl, null);
        }

        public static void RewriteSummary(this ILogger logger, string rewrittenUrl)
        {
            _rewriteSummary(logger, rewrittenUrl, null);
        }

        public static void AbortedRequest(this ILogger logger, string requestedUrl)
        {
            _abortedRequest(logger, requestedUrl, null);
        }

        public static void CustomResponse(this ILogger logger, string requestedUrl)
        {
            _customResponse(logger, requestedUrl, null);
        }
    }
}
