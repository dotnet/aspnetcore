// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

internal class HostingApplicationDiagnostics
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    // internal so it can be used in tests
    internal const string ActivityName = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
    private const string ActivityStartKey = ActivityName + ".Start";
    private const string ActivityStopKey = ActivityName + ".Stop";

    private const string DeprecatedDiagnosticsBeginRequestKey = "Microsoft.AspNetCore.Hosting.BeginRequest";
    private const string DeprecatedDiagnosticsEndRequestKey = "Microsoft.AspNetCore.Hosting.EndRequest";
    private const string DiagnosticsUnhandledExceptionKey = "Microsoft.AspNetCore.Hosting.UnhandledException";

    private readonly ActivitySource _activitySource;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly DistributedContextPropagator _propagator;
    private readonly ILogger _logger;

    public HostingApplicationDiagnostics(
        ILogger logger,
        DiagnosticListener diagnosticListener,
        ActivitySource activitySource,
        DistributedContextPropagator propagator)
    {
        _logger = logger;
        _diagnosticListener = diagnosticListener;
        _activitySource = activitySource;
        _propagator = propagator;
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
        var diagnosticListenerActivityCreationEnabled = (diagnosticListenerEnabled && _diagnosticListener.IsEnabled(ActivityName, httpContext));
        var loggingEnabled = _logger.IsEnabled(LogLevel.Critical);


        if (loggingEnabled || diagnosticListenerActivityCreationEnabled || _activitySource.HasListeners())
        {
            context.Activity = StartActivity(httpContext, loggingEnabled, diagnosticListenerActivityCreationEnabled, out var hasDiagnosticListener);
            context.HasDiagnosticListener = hasDiagnosticListener;

            if (context.Activity is Activity activity)
            {
                if (httpContext.Features.Get<IHttpActivityFeature>() is IHttpActivityFeature feature)
                {
                    feature.Activity = activity;
                }
                else
                {
                    httpContext.Features.Set(context.HttpActivityFeature);
                }
            }
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
            context.Scope = Log.RequestScope(_logger, httpContext);

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
    public void RequestEnd(HttpContext httpContext, Exception? exception, HostingApplication.Context context)
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
        if (activity is not null)
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
    public static void ContextDisposed(HostingApplication.Context context)
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
        var startLog = new HostingRequestStartingLog(context.HttpContext!);
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
    private Activity? StartActivity(HttpContext httpContext, bool loggingEnabled, bool diagnosticListenerActivityCreationEnabled, out bool hasDiagnosticListener)
    {
        var activity = _activitySource.CreateActivity(ActivityName, ActivityKind.Server);
        if (activity is null && (loggingEnabled || diagnosticListenerActivityCreationEnabled))
        {
            activity = new Activity(ActivityName);
        }
        hasDiagnosticListener = false;

        if (activity is null)
        {
            return null;
        }
        var headers = httpContext.Request.Headers;
        _propagator.ExtractTraceIdAndState(headers,
            static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                fieldValues = default;
                var headers = (IHeaderDictionary)carrier!;
                fieldValue = headers[fieldName];
            },
            out var requestId,
            out var traceState);

        if (!string.IsNullOrEmpty(requestId))
        {
            activity.SetParentId(requestId);
            if (!string.IsNullOrEmpty(traceState))
            {
                activity.TraceStateString = traceState;
            }
            var baggage = _propagator.ExtractBaggage(headers, static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                fieldValues = default;
                var headers = (IHeaderDictionary)carrier!;
                fieldValue = headers[fieldName];
            });

            // AddBaggage adds items at the beginning  of the list, so we need to add them in reverse to keep the same order as the client
            // By contract, the propagator has already reversed the order of items so we need not reverse it again 
            // Order could be important if baggage has two items with the same key (that is allowed by the contract)
            if (baggage is not null)
            {
                foreach (var baggageItem in baggage)
                {
                    activity.AddBaggage(baggageItem.Key, baggageItem.Value);
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

    private static class Log
    {
        public static IDisposable RequestScope(ILogger logger, HttpContext httpContext)
        {
            return logger.BeginScope(new HostingLogScope(httpContext));
        }

        private sealed class HostingLogScope : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly string _path;
            private readonly string _traceIdentifier;

            private string? _cachedToString;

            public int Count => 2;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>("RequestId", _traceIdentifier);
                    }
                    else if (index == 1)
                    {
                        return new KeyValuePair<string, object>("RequestPath", _path);
                    }

                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public HostingLogScope(HttpContext httpContext)
            {
                _traceIdentifier = httpContext.TraceIdentifier;
                _path = (httpContext.Request.PathBase.HasValue
                         ? httpContext.Request.PathBase + httpContext.Request.Path
                         : httpContext.Request.Path).ToString();
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = string.Format(
                        CultureInfo.InvariantCulture,
                        "RequestPath:{0} RequestId:{1}",
                        _path,
                        _traceIdentifier);
                }

                return _cachedToString;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (var i = 0; i < Count; ++i)
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
