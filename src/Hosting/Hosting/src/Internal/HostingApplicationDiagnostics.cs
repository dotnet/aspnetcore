// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Hosting
{
    internal class HostingApplicationDiagnostics
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private const string ActivityName = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
        private const string ActivityStartKey = ActivityName + ".Start";
        private const string ActivityStopKey = ActivityName + ".Stop";

        private const string DeprecatedDiagnosticsBeginRequestKey = "Microsoft.AspNetCore.Hosting.BeginRequest";
        private const string DeprecatedDiagnosticsEndRequestKey = "Microsoft.AspNetCore.Hosting.EndRequest";
        private const string DiagnosticsUnhandledExceptionKey = "Microsoft.AspNetCore.Hosting.UnhandledException";

        private readonly DiagnosticListener _diagnosticListener;
        private readonly ILogger _logger;

        public HostingApplicationDiagnostics(ILogger logger, DiagnosticListener diagnosticListener)
        {
            _logger = logger;
            _diagnosticListener = diagnosticListener;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginRequest(HttpContext httpContext, HostingApplication.Context context)
        {
            long startTimestamp = 0;

            if (HostingEventSource.Log.IsEnabled())
            {
                context.EventLogEnabled = true;
                // To keep the hot path short we defer logging in this function to non-inlines
                RecordRequestStartEventLog(httpContext);
            }

            var diagnosticListenerEnabled = _diagnosticListener.IsEnabled();
            var loggingEnabled = _logger.IsEnabled(LogLevel.Critical);

            if (loggingEnabled || (diagnosticListenerEnabled && _diagnosticListener.IsEnabled(ActivityName, httpContext)))
            {
                context.Activity = StartActivity(httpContext, out var hasDiagnosticListener);
                context.HasDiagnosticListener = hasDiagnosticListener;
            }

            if (diagnosticListenerEnabled)
            {
                if (_diagnosticListener.IsEnabled(DeprecatedDiagnosticsBeginRequestKey))
                {
                    startTimestamp = Stopwatch.GetTimestamp();
                    RecordBeginRequestDiagnostics(httpContext, startTimestamp);
                }
            }

            // To avoid allocation, return a null scope if the logger is not on at least to some degree.
            if (loggingEnabled)
            {
                // Scope may be relevant for a different level of logging, so we always create it
                // see: https://github.com/aspnet/Hosting/pull/944
                // Scope can be null if logging is not on.
                context.Scope = _logger.RequestScope(httpContext, context.Activity);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    if (startTimestamp == 0)
                    {
                        startTimestamp = Stopwatch.GetTimestamp();
                    }

                    // Non-inline
                    LogRequestStarting(context);
                }
            }
            context.StartTimestamp = startTimestamp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RequestEnd(HttpContext httpContext, Exception exception, HostingApplication.Context context)
        {
            // Local cache items resolved multiple items, in order of use so they are primed in cpu pipeline when used
            var startTimestamp = context.StartTimestamp;
            long currentTimestamp = 0;

            // If startTimestamp was 0, then Information logging wasn't enabled at for this request (and calculated time will be wildly wrong)
            // Is used as proxy to reduce calls to virtual: _logger.IsEnabled(LogLevel.Information)
            if (startTimestamp != 0)
            {
                currentTimestamp = Stopwatch.GetTimestamp();
                // Non-inline
                LogRequestFinished(context, startTimestamp, currentTimestamp);
            }

            if (_diagnosticListener.IsEnabled())
            {
                if (currentTimestamp == 0)
                {
                    currentTimestamp = Stopwatch.GetTimestamp();
                }

                if (exception == null)
                {
                    // No exception was thrown, request was successful
                    if (_diagnosticListener.IsEnabled(DeprecatedDiagnosticsEndRequestKey))
                    {
                        // Diagnostics is enabled for EndRequest, but it may not be for BeginRequest
                        // so call GetTimestamp if currentTimestamp is zero (from above)
                        RecordEndRequestDiagnostics(httpContext, currentTimestamp);
                    }
                }
                else
                {
                    // Exception was thrown from request
                    if (_diagnosticListener.IsEnabled(DiagnosticsUnhandledExceptionKey))
                    {
                        // Diagnostics is enabled for UnhandledException, but it may not be for BeginRequest
                        // so call GetTimestamp if currentTimestamp is zero (from above)
                        RecordUnhandledExceptionDiagnostics(httpContext, currentTimestamp, exception);
                    }

                }
            }

            var activity = context.Activity;
            // Always stop activity if it was started
            if (activity != null)
            {
                StopActivity(httpContext, activity, context.HasDiagnosticListener);
            }

            if (context.EventLogEnabled)
            {
                if (exception != null)
                {
                    // Non-inline
                    HostingEventSource.Log.UnhandledException();
                }

                // Count 500 as failed requests
                if (httpContext.Response.StatusCode >= 500)
                {
                    HostingEventSource.Log.RequestFailed();
                }
            }

            // Logging Scope is finshed with
            context.Scope?.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ContextDisposed(HostingApplication.Context context)
        {
            if (context.EventLogEnabled)
            {
                // Non-inline
                HostingEventSource.Log.RequestStop();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRequestStarting(HostingApplication.Context context)
        {
            // IsEnabled is checked in the caller, so if we are here just log
            var startLog = new HostingRequestStartingLog(context.HttpContext);
            context.StartLog = startLog;

            _logger.Log(
                logLevel: LogLevel.Information,
                eventId: LoggerEventIds.RequestStarting,
                state: startLog,
                exception: null,
                formatter: HostingRequestStartingLog.Callback);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRequestFinished(HostingApplication.Context context, long startTimestamp, long currentTimestamp)
        {
            // IsEnabled isn't checked in the caller, startTimestamp > 0 is used as a fast proxy check
            // but that may be because diagnostics are enabled, which also uses startTimestamp,
            // so check if we logged the start event
            if (context.StartLog != null)
            {
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));

                _logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: LoggerEventIds.RequestFinished,
                    state: new HostingRequestFinishedLog(context, elapsed),
                    exception: null,
                    formatter: HostingRequestFinishedLog.Callback);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RecordBeginRequestDiagnostics(HttpContext httpContext, long startTimestamp)
        {
            _diagnosticListener.Write(
                DeprecatedDiagnosticsBeginRequestKey,
                new
                {
                    httpContext = httpContext,
                    timestamp = startTimestamp
                });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RecordEndRequestDiagnostics(HttpContext httpContext, long currentTimestamp)
        {
            _diagnosticListener.Write(
                DeprecatedDiagnosticsEndRequestKey,
                new
                {
                    httpContext = httpContext,
                    timestamp = currentTimestamp
                });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RecordUnhandledExceptionDiagnostics(HttpContext httpContext, long currentTimestamp, Exception exception)
        {
            _diagnosticListener.Write(
                DiagnosticsUnhandledExceptionKey,
                new
                {
                    httpContext = httpContext,
                    timestamp = currentTimestamp,
                    exception = exception
                });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RecordRequestStartEventLog(HttpContext httpContext)
        {
            HostingEventSource.Log.RequestStart(httpContext.Request.Method, httpContext.Request.Path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Activity StartActivity(HttpContext httpContext, out bool hasDiagnosticListener)
        {
            var activity = new Activity(ActivityName);
            hasDiagnosticListener = false;

            var headers = httpContext.Request.Headers;
            if (!headers.TryGetValue(HeaderNames.TraceParent, out var requestId))
            {
                headers.TryGetValue(HeaderNames.RequestId, out requestId);
            }

            if (!StringValues.IsNullOrEmpty(requestId))
            {
                activity.SetParentId(requestId);
                if (headers.TryGetValue(HeaderNames.TraceState, out var traceState))
                {
                    activity.TraceStateString = traceState;
                }

                // We expect baggage to be empty by default
                // Only very advanced users will be using it in near future, we encourage them to keep baggage small (few items)
                string[] baggage = headers.GetCommaSeparatedValues(HeaderNames.CorrelationContext);
                if (baggage.Length > 0)
                {
                    foreach (var item in baggage)
                    {
                        if (NameValueHeaderValue.TryParse(item, out var baggageItem))
                        {
                            activity.AddBaggage(baggageItem.Name.ToString(), HttpUtility.UrlDecode(baggageItem.Value.ToString()));
                        }
                    }
                }
            }

            _diagnosticListener.OnActivityImport(activity, httpContext);

            if (_diagnosticListener.IsEnabled(ActivityStartKey))
            {
                hasDiagnosticListener = true;
                StartActivity(activity, httpContext);
            }
            else
            {
                activity.Start();
            }

            return activity;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StopActivity(HttpContext httpContext, Activity activity, bool hasDiagnosticListener)
        {
            if (hasDiagnosticListener)
            {
                StopActivity(activity, httpContext);
            }
            else
            {
                activity.Stop();
            }
        }

        // These are versions of DiagnosticSource.Start/StopActivity that don't allocate strings per call (see https://github.com/dotnet/corefx/issues/37055)
        private Activity StartActivity(Activity activity, HttpContext httpContext)
        {
            activity.Start();
            _diagnosticListener.Write(ActivityStartKey, httpContext);
            return activity;
        }

        private void StopActivity(Activity activity, HttpContext httpContext)
        {
            // Stop sets the end time if it was unset, but we want it set before we issue the write
            // so we do it now.
            if (activity.Duration == TimeSpan.Zero)
            {
                activity.SetEndTime(DateTime.UtcNow);
            }
            _diagnosticListener.Write(ActivityStopKey, httpContext);
            activity.Stop();    // Resets Activity.Current (we want this after the Write)
        }
    }
}
