// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Hosting.Internal
{
    internal static class HostingLoggerExtensions
    {
        public static IDisposable RequestScope(this ILogger logger, HttpContext httpContext)
        {
            return logger.BeginScopeImpl(new HostingLogScope(httpContext));
        }

        public static void RequestStarting(this ILogger logger, HttpContext httpContext)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: LoggerEventIds.RequestStarting,
                    state: new HostingRequestStarting(httpContext),
                    exception: null,
                    formatter: HostingRequestStarting.Callback);
            }
        }

        public static void RequestFinished(this ILogger logger, HttpContext httpContext, int startTimeInTicks)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var elapsed = new TimeSpan(Environment.TickCount - startTimeInTicks);
                logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: LoggerEventIds.RequestFinished,
                    state: new HostingRequestFinished(httpContext, elapsed),
                    exception: null,
                    formatter: HostingRequestFinished.Callback);
            }
        }

        public static void RequestFailed(this ILogger logger, HttpContext httpContext, int startTimeInTicks)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var elapsed = new TimeSpan(Environment.TickCount - startTimeInTicks);
                logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: LoggerEventIds.RequestFailed,
                    state: new HostingRequestFailed(httpContext, elapsed),
                    exception: null,
                    formatter: HostingRequestFailed.Callback);
            }
        }

        public static void ApplicationError(this ILogger logger, Exception exception)
        {
            logger.LogError(
                eventId: LoggerEventIds.ApplicationStartupException,
                message: "Application startup exception",
                error: exception);
        }

        public static void Starting(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Verbose))
            {
                logger.LogVerbose(
                   eventId: LoggerEventIds.Starting,
                   data: "Hosting starting");
            }
        }

        public static void Started(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Verbose))
            {
                logger.LogVerbose(
                    eventId: LoggerEventIds.Started,
                    data: "Hosting started");
            }
        }

        public static void Shutdown(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Verbose))
            {
                logger.LogVerbose(
                    eventId: LoggerEventIds.Shutdown,
                    data: "Hosting shutdown");
            }
        }


        private class HostingLogScope : ILogValues
        {
            private readonly HttpContext _httpContext;

            private string _cachedToString;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;

            public HostingLogScope(HttpContext httpContext)
            {
                _httpContext = httpContext;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = $"RequestId:{_httpContext.TraceIdentifier} RequestPath:{_httpContext.Request.Path}";
                }

                return _cachedToString;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                if (_cachedGetValues == null)
                {
                    _cachedGetValues = new[]
                    {
                        new KeyValuePair<string, object>("RequestId", _httpContext.TraceIdentifier),
                        new KeyValuePair<string, object>("RequestPath", _httpContext.Request.Path.ToString()),
                    };
                }

                return _cachedGetValues;
            }
        }

        private class HostingRequestStarting : ILogValues
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestStarting)state).ToString();

            private readonly HttpRequest _request;

            private string _cachedToString;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;

            public HostingRequestStarting(HttpContext httpContext)
            {
                _request = httpContext.Request;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = $"Request starting {_request.Protocol} {_request.Method} {_request.Scheme}://{_request.Host}{_request.PathBase}{_request.Path}{_request.QueryString} {_request.ContentType} {_request.ContentLength}";
                }

                return _cachedToString;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                if (_cachedGetValues == null)
                {
                    _cachedGetValues = new[]
                    {
                        new KeyValuePair<string, object>("Protocol", _request.Protocol),
                        new KeyValuePair<string, object>("Method", _request.Method),
                        new KeyValuePair<string, object>("ContentType", _request.ContentType),
                        new KeyValuePair<string, object>("ContentLength", _request.ContentLength),
                        new KeyValuePair<string, object>("Scheme", _request.Scheme.ToString()),
                        new KeyValuePair<string, object>("Host", _request.Host.ToString()),
                        new KeyValuePair<string, object>("PathBase", _request.PathBase.ToString()),
                        new KeyValuePair<string, object>("Path", _request.Path.ToString()),
                        new KeyValuePair<string, object>("QueryString", _request.QueryString.ToString()),
                    };
                }

                return _cachedGetValues;
            }
        }

        private class HostingRequestFinished
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFinished)state).ToString();

            private readonly HttpContext _httpContext;
            private readonly TimeSpan _elapsed;

            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;
            private string _cachedToString;

            public HostingRequestFinished(HttpContext httpContext, TimeSpan elapsed)
            {
                _httpContext = httpContext;
                _elapsed = elapsed;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = $"Request finished in {_elapsed.TotalMilliseconds}ms {_httpContext.Response.StatusCode} {_httpContext.Response.ContentType}";
                }

                return _cachedToString;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                if (_cachedGetValues == null)
                {
                    _cachedGetValues = new[]
                    {
                        new KeyValuePair<string, object>("ElapsedMilliseconds", _elapsed.TotalMilliseconds),
                        new KeyValuePair<string, object>("StatusCode", _httpContext.Response.StatusCode),
                        new KeyValuePair<string, object>("ContentType", _httpContext.Response.ContentType),
                    };
                }

                return _cachedGetValues;
            }
        }

        private class HostingRequestFailed
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFailed)state).ToString();

            private readonly HttpContext _httpContext;
            private readonly TimeSpan _elapsed;

            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;
            private string _cachedToString;

            public HostingRequestFailed(HttpContext httpContext, TimeSpan elapsed)
            {
                _httpContext = httpContext;
                _elapsed = elapsed;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = $"Request finished in {_elapsed.TotalMilliseconds}ms 500";
                }

                return _cachedToString;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                if (_cachedGetValues == null)
                {
                    _cachedGetValues = new[]
                    {
                        new KeyValuePair<string, object>("ElapsedMilliseconds", _elapsed.TotalMilliseconds),
                        new KeyValuePair<string, object>("StatusCode", 500),
                        new KeyValuePair<string, object>("ContentType", null),
                    };
                }

                return _cachedGetValues;
            }
        }
    }
}

