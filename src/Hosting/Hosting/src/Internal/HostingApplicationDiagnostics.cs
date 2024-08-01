// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class HostingApplicationDiagnostics
{
    // internal so it can be used in tests
    internal const string ActivityName = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
    private const string ActivityStartKey = ActivityName + ".Start";
    private const string ActivityStopKey = ActivityName + ".Stop";

    private const string DeprecatedDiagnosticsBeginRequestKey = "Microsoft.AspNetCore.Hosting.BeginRequest";
    private const string DeprecatedDiagnosticsEndRequestKey = "Microsoft.AspNetCore.Hosting.EndRequest";
    private const string DiagnosticsUnhandledExceptionKey = "Microsoft.AspNetCore.Hosting.UnhandledException";

    private const string RequestUnhandledKey = "__RequestUnhandled";

    private readonly ActivitySource _activitySource;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly DistributedContextPropagator _propagator;
    private readonly HostingEventSource _eventSource;
    private readonly HostingMetrics _metrics;
    private readonly ILogger _logger;

    public HostingApplicationDiagnostics(
        ILogger logger,
        DiagnosticListener diagnosticListener,
        ActivitySource activitySource,
        DistributedContextPropagator propagator,
        HostingEventSource eventSource,
        HostingMetrics metrics)
    {
        _logger = logger;
        _diagnosticListener = diagnosticListener;
        _activitySource = activitySource;
        _propagator = propagator;
        _eventSource = eventSource;
        _metrics = metrics;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginRequest(HttpContext httpContext, HostingApplication.Context context)
    {
        long startTimestamp = 0;

        if (_metrics.IsEnabled())
        {
            context.MetricsEnabled = true;
            context.MetricsTagsFeature ??= new HttpMetricsTagsFeature();
            httpContext.Features.Set<IHttpMetricsTagsFeature>(context.MetricsTagsFeature);

            context.MetricsTagsFeature.Method = httpContext.Request.Method;
            context.MetricsTagsFeature.Protocol = httpContext.Request.Protocol;
            context.MetricsTagsFeature.Scheme = httpContext.Request.Scheme;

            startTimestamp = Stopwatch.GetTimestamp();

            // To keep the hot path short we defer logging in this function to non-inlines
            RecordRequestStartMetrics(httpContext);
        }

        if (_eventSource.IsEnabled())
        {
            context.EventLogEnabled = true;

            if (startTimestamp == 0)
            {
                startTimestamp = Stopwatch.GetTimestamp();
            }

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

            if (context.Activity != null)
            {
                httpContext.Features.Set<IHttpActivityFeature>(context.HttpActivityFeature);
            }
        }

        if (diagnosticListenerEnabled)
        {
            if (_diagnosticListener.IsEnabled(DeprecatedDiagnosticsBeginRequestKey))
            {
                if (startTimestamp == 0)
                {
                    startTimestamp = Stopwatch.GetTimestamp();
                }

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

        // startTimestamp has a value if:
        // - Information logging was enabled at for this request (and calculated time will be wildly wrong)
        //   Is used as proxy to reduce calls to virtual: _logger.IsEnabled(LogLevel.Information)
        // - EventLog or metrics was enabled
        if (startTimestamp != 0)
        {
            currentTimestamp = Stopwatch.GetTimestamp();
            var reachedPipelineEnd = httpContext.Items.ContainsKey(RequestUnhandledKey);

            // Non-inline
            LogRequestFinished(context, startTimestamp, currentTimestamp);

            if (context.MetricsEnabled)
            {
                Debug.Assert(context.MetricsTagsFeature != null, "MetricsTagsFeature should be set if MetricsEnabled is true.");

                var endpoint = HttpExtensions.GetOriginalEndpoint(httpContext);
                var disableHttpRequestDurationMetric = endpoint?.Metadata.GetMetadata<IDisableHttpMetricsMetadata>() != null || context.MetricsTagsFeature.MetricsDisabled;
                var route = endpoint?.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()?.Route;

                _metrics.RequestEnd(
                    context.MetricsTagsFeature.Protocol!,
                    context.MetricsTagsFeature.Scheme!,
                    context.MetricsTagsFeature.Method!,
                    route,
                    httpContext.Response.StatusCode,
                    reachedPipelineEnd,
                    exception,
                    context.MetricsTagsFeature.TagsList,
                    startTimestamp,
                    currentTimestamp,
                    disableHttpRequestDurationMetric);
            }

            if (reachedPipelineEnd)
            {
                LogRequestUnhandled(context);
            }
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
        // Always stop activity if it was started.
        // The HTTP activity must be stopped after the HTTP request duration metric is recorded.
        // This order means the activity is ongoing while the metric is recorded and libraries like OTEL
        // can capture the activity as a metric exemplar.
        if (activity is not null)
        {
            StopActivity(httpContext, activity, context.HasDiagnosticListener);
        }

        if (context.EventLogEnabled)
        {
            if (exception != null)
            {
                // Non-inline
                _eventSource.UnhandledException();
            }

            // Count 500 as failed requests
            if (httpContext.Response.StatusCode >= 500)
            {
                _eventSource.RequestFailed();
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
            _eventSource.RequestStop();
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
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);

            _logger.Log(
                logLevel: LogLevel.Information,
                eventId: LoggerEventIds.RequestFinished,
                state: new HostingRequestFinishedLog(context, elapsed),
                exception: null,
                formatter: HostingRequestFinishedLog.Callback);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LogRequestUnhandled(HostingApplication.Context context)
    {
        _logger.Log(
            logLevel: LogLevel.Information,
            eventId: LoggerEventIds.RequestUnhandled,
            state: new HostingRequestUnhandledLog(context.HttpContext!),
            exception: null,
            formatter: HostingRequestUnhandledLog.Callback);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "The values being passed into Write have the commonly used properties being preserved with DynamicDependency.")]
    private static void WriteDiagnosticEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        DiagnosticSource diagnosticSource, string name, TValue value)
    {
        diagnosticSource.Write(name, value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecordBeginRequestDiagnostics(HttpContext httpContext, long startTimestamp)
    {
        WriteDiagnosticEvent(
            _diagnosticListener,
            DeprecatedDiagnosticsBeginRequestKey,
            new DeprecatedRequestData(httpContext, startTimestamp));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecordEndRequestDiagnostics(HttpContext httpContext, long currentTimestamp)
    {
        WriteDiagnosticEvent(
            _diagnosticListener,
            DeprecatedDiagnosticsEndRequestKey,
            new DeprecatedRequestData(httpContext, currentTimestamp));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecordUnhandledExceptionDiagnostics(HttpContext httpContext, long currentTimestamp, Exception exception)
    {
        WriteDiagnosticEvent(
            _diagnosticListener,
            DiagnosticsUnhandledExceptionKey,
            new UnhandledExceptionData(httpContext, currentTimestamp, exception));
    }

    private sealed class DeprecatedRequestData
    {
        // Common properties. Properties not in this list could be trimmed.
        [DynamicDependency(nameof(HttpContext.Request), typeof(HttpContext))]
        [DynamicDependency(nameof(HttpContext.Response), typeof(HttpContext))]
        [DynamicDependency(nameof(HttpRequest.Path), typeof(HttpRequest))]
        [DynamicDependency(nameof(HttpRequest.Method), typeof(HttpRequest))]
        [DynamicDependency(nameof(HttpResponse.StatusCode), typeof(HttpResponse))]
        internal DeprecatedRequestData(HttpContext httpContext, long timestamp)
        {
            this.httpContext = httpContext;
            this.timestamp = timestamp;
        }

        // Compatibility with anonymous object property names
        public HttpContext httpContext { get; }
        public long timestamp { get; }

        public override string ToString() => $"{{ {nameof(httpContext)} = {httpContext}, {nameof(timestamp)} = {timestamp} }}";
    }

    private sealed class UnhandledExceptionData
    {
        // Common properties. Properties not in this list could be trimmed.
        [DynamicDependency(nameof(HttpContext.Request), typeof(HttpContext))]
        [DynamicDependency(nameof(HttpContext.Response), typeof(HttpContext))]
        [DynamicDependency(nameof(HttpRequest.Path), typeof(HttpRequest))]
        [DynamicDependency(nameof(HttpRequest.Method), typeof(HttpRequest))]
        [DynamicDependency(nameof(HttpResponse.StatusCode), typeof(HttpResponse))]
        internal UnhandledExceptionData(HttpContext httpContext, long timestamp, Exception exception)
        {
            this.httpContext = httpContext;
            this.timestamp = timestamp;
            this.exception = exception;
        }

        // Compatibility with anonymous object property names
        public HttpContext httpContext { get; }
        public long timestamp { get; }
        public Exception exception { get; }

        public override string ToString() => $"{{ {nameof(httpContext)} = {httpContext}, {nameof(timestamp)} = {timestamp}, {nameof(exception)} = {exception} }}";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecordRequestStartEventLog(HttpContext httpContext)
    {
        _eventSource.RequestStart(httpContext.Request.Method, httpContext.Request.Path);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecordRequestStartMetrics(HttpContext httpContext)
    {
        _metrics.RequestStart(httpContext.Request.Scheme, httpContext.Request.Method);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Activity? StartActivity(HttpContext httpContext, bool loggingEnabled, bool diagnosticListenerActivityCreationEnabled, out bool hasDiagnosticListener)
    {
        hasDiagnosticListener = false;

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

        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            if (ActivityContext.TryParse(requestId, traceState, isRemote: true, out ActivityContext context))
            {
                // The requestId used the W3C ID format. Unfortunately, the ActivitySource.CreateActivity overload that
                // takes a string parentId never sets HasRemoteParent to true. We work around that by calling the
                // ActivityContext overload instead which sets HasRemoteParent to parentContext.IsRemote.
                // https://github.com/dotnet/aspnetcore/pull/41568#discussion_r868733305
                activity = _activitySource.CreateActivity(ActivityName, ActivityKind.Server, context);
            }
            else
            {
                // Pass in the ID we got from the headers if there was one.
                activity = _activitySource.CreateActivity(ActivityName, ActivityKind.Server, string.IsNullOrEmpty(requestId) ? null! : requestId);
            }
        }

        if (activity is null)
        {
            // CreateActivity didn't create an Activity (this is an optimization for the
            // case when there are no listeners). Let's create it here if needed.
            if (loggingEnabled || diagnosticListenerActivityCreationEnabled)
            {
                activity = new Activity(ActivityName);
                if (!string.IsNullOrEmpty(requestId))
                {
                    activity.SetParentId(requestId);
                }
            }
            else
            {
                return null;
            }
        }

        // The trace id was successfully extracted, so we can set the trace state
        // https://www.w3.org/TR/trace-context/#tracestate-header
        if (!string.IsNullOrEmpty(requestId))
        {
            if (!string.IsNullOrEmpty(traceState))
            {
                activity.TraceStateString = traceState;
            }
        }

        // Baggage can be used regardless of whether a distributed trace id was present on the inbound request.
        // https://www.w3.org/TR/baggage/#abstract
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
    // DynamicDependency matches the properties selected in:
    // https://github.com/dotnet/diagnostics/blob/7cc6fbef613cdfe5ff64393120d59d7a15e98bd6/src/Microsoft.Diagnostics.Monitoring.EventPipe/Configuration/HttpRequestSourceConfiguration.cs#L20-L33
    [DynamicDependency(nameof(HttpContext.Request), typeof(HttpContext))]
    [DynamicDependency(nameof(HttpRequest.Scheme), typeof(HttpRequest))]
    [DynamicDependency(nameof(HttpRequest.Host), typeof(HttpRequest))]
    [DynamicDependency(nameof(HttpRequest.PathBase), typeof(HttpRequest))]
    [DynamicDependency(nameof(HttpRequest.QueryString), typeof(HttpRequest))]
    [DynamicDependency(nameof(HttpRequest.Path), typeof(HttpRequest))]
    [DynamicDependency(nameof(HttpRequest.Method), typeof(HttpRequest))]
    [DynamicDependency(nameof(HttpRequest.Headers), typeof(HttpRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(QueryString))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HostString))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PathString))]
    // OpenTelemetry gets the context from the context using the DefaultHttpContext.HttpContext property.
    [DynamicDependency(nameof(DefaultHttpContext.HttpContext), typeof(DefaultHttpContext))]
    private Activity StartActivity(Activity activity, HttpContext httpContext)
    {
        activity.Start();
        WriteDiagnosticEvent(_diagnosticListener, ActivityStartKey, httpContext);
        return activity;
    }

    // DynamicDependency matches the properties selected in:
    // https://github.com/dotnet/diagnostics/blob/7cc6fbef613cdfe5ff64393120d59d7a15e98bd6/src/Microsoft.Diagnostics.Monitoring.EventPipe/Configuration/HttpRequestSourceConfiguration.cs#L35-L38
    [DynamicDependency(nameof(HttpContext.Response), typeof(HttpContext))]
    [DynamicDependency(nameof(HttpResponse.StatusCode), typeof(HttpResponse))]
    [DynamicDependency(nameof(HttpResponse.Headers), typeof(HttpResponse))]
    // OpenTelemetry gets the context from the context using the DefaultHttpContext.HttpContext property.
    [DynamicDependency(nameof(DefaultHttpContext.HttpContext), typeof(DefaultHttpContext))]
    private void StopActivity(Activity activity, HttpContext httpContext)
    {
        // Stop sets the end time if it was unset, but we want it set before we issue the write
        // so we do it now.
        if (activity.Duration == TimeSpan.Zero)
        {
            activity.SetEndTime(DateTime.UtcNow);
        }
        WriteDiagnosticEvent(_diagnosticListener, ActivityStopKey, httpContext);
        activity.Stop();    // Resets Activity.Current (we want this after the Write)
    }

    private static class Log
    {
        public static IDisposable? RequestScope(ILogger logger, HttpContext httpContext)
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
