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
        private static readonly Action<ILogger, string, Exception> _urlRewriteNotMatchedRule;
        private static readonly Action<ILogger, string, Exception> _urlRewriteMatchedRule;
        private static readonly Action<ILogger, Exception> _modRewriteNotMatchedRule;
        private static readonly Action<ILogger, Exception> _modRewriteMatchedRule;
        private static readonly Action<ILogger, Exception> _redirectedToHttps;
        private static readonly Action<ILogger, Exception> _redirectedToWww;
        private static readonly Action<ILogger, Exception> _redirectedToNonWww;
        private static readonly Action<ILogger, string, Exception> _redirectedRequest;
        private static readonly Action<ILogger, string, Exception> _rewrittenRequest;
        private static readonly Action<ILogger, string, Exception> _abortedRequest;
        private static readonly Action<ILogger, string, Exception> _customResponse;

        static RewriteMiddlewareLoggingExtensions()
        {
            _requestContinueResults = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            new EventId(1, "RequestContinueResults"),
                            "Request is continuing in applying rules. Current url is {currentUrl}");

            _requestResponseComplete = LoggerMessage.Define<string, int>(
                            LogLevel.Debug,
                            new EventId(2, "RequestResponseComplete"),
                            "Request is done processing. Location header '{Location}' with status code '{StatusCode}'.");

            _requestStopRules = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            new EventId(3, "RequestStopRules"),
                            "Request is done applying rules. Url was rewritten to {rewrittenUrl}");

            _urlRewriteNotMatchedRule = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            new EventId(4, "UrlRewriteNotMatchedRule"),
                            "Request did not match current rule '{Name}'.");

            _urlRewriteMatchedRule = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            new EventId(5, "UrlRewriteMatchedRule"),
                            "Request matched current UrlRewriteRule '{Name}'.");

            _modRewriteNotMatchedRule = LoggerMessage.Define(
                            LogLevel.Debug,
                            new EventId(6, "ModRewriteNotMatchedRule"),
                            "Request matched current ModRewriteRule.");

            _modRewriteMatchedRule = LoggerMessage.Define(
                            LogLevel.Debug,
                            new EventId(7, "ModRewriteMatchedRule"),
                            "Request matched current ModRewriteRule.");

            _redirectedToHttps = LoggerMessage.Define(
                            LogLevel.Information,
                            new EventId(8, "RedirectedToHttps"),
                            "Request redirected to HTTPS");

            _redirectedRequest = LoggerMessage.Define<string>(
                            LogLevel.Information,
                            new EventId(9, "RedirectedRequest"),
                            "Request was redirected to {redirectedUrl}");

            _rewrittenRequest = LoggerMessage.Define<string>(
                            LogLevel.Information,
                            new EventId(10, "RewritetenRequest"),
                            "Request was rewritten to {rewrittenUrl}");

            _abortedRequest = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            new EventId(11, "AbortedRequest"),
                            "Request to {requestedUrl} was aborted");

            _customResponse = LoggerMessage.Define<string>(
                            LogLevel.Debug,
                            new EventId(12, "CustomResponse"),
                            "Request to {requestedUrl} was ended");

            _redirectedToWww = LoggerMessage.Define(
                            LogLevel.Information,
                            new EventId(13, "RedirectedToWww"),
                            "Request redirected to www");

            _redirectedToNonWww = LoggerMessage.Define(
                            LogLevel.Information,
                            new EventId(14, "RedirectedToNonWww"),
                            "Request redirected to root domain from www subdomain");
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

        public static void UrlRewriteNotMatchedRule(this ILogger logger, string name)
        {
            _urlRewriteNotMatchedRule(logger, name, null);
        }

        public static void UrlRewriteMatchedRule(this ILogger logger, string name)
        {
            _urlRewriteMatchedRule(logger, name, null);
        }

        public static void ModRewriteNotMatchedRule(this ILogger logger)
        {
            _modRewriteNotMatchedRule(logger, null);
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

        public static void RedirectedToNonWww(this ILogger logger)
        {
            _redirectedToNonWww(logger, null);
        }

        public static void RedirectedRequest(this ILogger logger, string redirectedUrl)
        {
            _redirectedRequest(logger, redirectedUrl, null);
        }

        public static void RewrittenRequest(this ILogger logger, string rewrittenUrl)
        {
            _rewrittenRequest(logger, rewrittenUrl, null);
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
