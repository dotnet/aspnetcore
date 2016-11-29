// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal static class HostingLoggerExtensions
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        public static IDisposable RequestScope(this ILogger logger, HttpContext httpContext)
        {
            return logger.BeginScope(new HostingLogScope(httpContext));
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

        public static void RequestFinished(this ILogger logger, HttpContext httpContext, long startTimestamp, long currentTimestamp)
        {
            // Don't log if Information logging wasn't enabled at start or end of request as time will be wildly wrong.
            if (startTimestamp != 0)
            {
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));

                logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: LoggerEventIds.RequestFinished,
                    state: new HostingRequestFinished(httpContext, elapsed),
                    exception: null,
                    formatter: HostingRequestFinished.Callback);
            }
        }

        public static void ApplicationError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError(
                eventId: LoggerEventIds.ApplicationStartupException,
                message: "Application startup exception",
                exception: exception);
        }

        public static void ApplicationError(this ILogger logger, EventId eventId, string message, Exception exception)
        {
            var reflectionTypeLoadException = exception as ReflectionTypeLoadException;
            if (reflectionTypeLoadException != null)
            {
                foreach (var ex in reflectionTypeLoadException.LoaderExceptions)
                {
                    message = message + Environment.NewLine + ex.Message;
                }
            }

            logger.LogCritical(
                eventId: eventId,
                message: message,
                exception: exception);
        }

        public static void Starting(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                   eventId: LoggerEventIds.Starting,
                   message: "Hosting starting");
            }
        }

        public static void Started(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    eventId: LoggerEventIds.Started,
                    message: "Hosting started");
            }
        }

        public static void Shutdown(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    eventId: LoggerEventIds.Shutdown,
                    message: "Hosting shutdown");
            }
        }


        private class HostingLogScope : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly HttpContext _httpContext;

            private string _cachedToString;

            public int Count
            {
                get
                {
                    return 2;
                }
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>("RequestId", _httpContext.TraceIdentifier);
                    }
                    else if (index == 1)
                    {
                        return new KeyValuePair<string, object>("RequestPath", _httpContext.Request.Path.ToString());
                    }
                    throw new IndexOutOfRangeException(nameof(index));
                }
            }

            public HostingLogScope(HttpContext httpContext)
            {
                _httpContext = httpContext;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = string.Format(
                        CultureInfo.InvariantCulture,
                        "RequestId:{0} RequestPath:{1}",
                        _httpContext.TraceIdentifier,
                        _httpContext.Request.Path);
                }

                return _cachedToString;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class HostingRequestStarting : IReadOnlyList<KeyValuePair<string, object>>
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestStarting)state).ToString();

            private readonly HttpRequest _request;

            private string _cachedToString;

            public int Count
            {
                get
                {
                    return 9;
                }
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>("Protocol", _request.Protocol);
                        case 1:
                            return new KeyValuePair<string, object>("Method", _request.Method);
                        case 2:
                            return new KeyValuePair<string, object>("ContentType", _request.ContentType);
                        case 3:
                            return new KeyValuePair<string, object>("ContentLength", _request.ContentLength);
                        case 4:
                            return new KeyValuePair<string, object>("Scheme", _request.Scheme.ToString());
                        case 5:
                            return new KeyValuePair<string, object>("Host", _request.Host.ToString());
                        case 6:
                            return new KeyValuePair<string, object>("PathBase", _request.PathBase.ToString());
                        case 7:
                            return new KeyValuePair<string, object>("Path", _request.Path.ToString());
                        case 8:
                            return new KeyValuePair<string, object>("QueryString", _request.QueryString.ToString());
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public HostingRequestStarting(HttpContext httpContext)
            {
                _request = httpContext.Request;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = string.Format(
                        CultureInfo.InvariantCulture,
                        "Request starting {0} {1} {2}://{3}{4}{5}{6} {7} {8}",
                        _request.Protocol,
                        _request.Method,
                        _request.Scheme,
                        _request.Host,
                        _request.PathBase,
                        _request.Path,
                        _request.QueryString,
                        _request.ContentType,
                        _request.ContentLength);
                }

                return _cachedToString;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class HostingRequestFinished : IReadOnlyList<KeyValuePair<string, object>>
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFinished)state).ToString();

            private readonly HttpContext _httpContext;
            private readonly TimeSpan _elapsed;

            private string _cachedToString;

            public int Count
            {
                get
                {
                    return 3;
                }
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>("ElapsedMilliseconds", _elapsed.TotalMilliseconds);
                        case 1:
                            return new KeyValuePair<string, object>("StatusCode", _httpContext.Response.StatusCode);
                        case 2:
                            return new KeyValuePair<string, object>("ContentType", _httpContext.Response.ContentType);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public HostingRequestFinished(HttpContext httpContext, TimeSpan elapsed)
            {
                _httpContext = httpContext;
                _elapsed = elapsed;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = string.Format(
                        CultureInfo.InvariantCulture,
                        "Request finished in {0}ms {1} {2}",
                        _elapsed.TotalMilliseconds,
                        _httpContext.Response.StatusCode,
                        _httpContext.Response.ContentType);
                }

                return _cachedToString;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

