using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.WebListener
{
    using AppFunc = Func<object, Task>;
    using LoggerFactoryFunc = Func<string, Func<TraceEventType, int, object, Exception, Func<object, Exception, string>, bool>>;
    using LoggerFunc = Func<TraceEventType, int, object, Exception, Func<object, Exception, string>, bool>;
    using System.Threading;

    public class WebListenerWrapper : IDisposable
    {
        private static readonly int DefaultMaxAccepts = 5 * Environment.ProcessorCount;

        private OwinWebListener _listener;
        private AppFunc _appFunc;
        private LoggerFunc _logger;

        private PumpLimits _pumpLimits;
        private int _acceptorCounts;
        private Action<object> _processRequest;

        // TODO: private IDictionary<string, object> _capabilities;

        internal WebListenerWrapper(OwinWebListener listener)
        {
            Contract.Assert(listener != null);
            _listener = listener;

            _processRequest = new Action<object>(ProcessRequestAsync);
            _pumpLimits = new PumpLimits(DefaultMaxAccepts);
        }

        internal void Start(AppFunc app, IList<IDictionary<string, object>> addresses,  LoggerFactoryFunc loggerFactory)
        {
            // Can't call Start twice
            Contract.Assert(_appFunc == null);

            Contract.Assert(app != null);
            Contract.Assert(addresses != null);

            _appFunc = app;
            _logger = LogHelper.CreateLogger(loggerFactory, typeof(WebListenerWrapper));
            LogHelper.LogInfo(_logger, "Start");

            foreach (var address in addresses)
            {
                // Build addresses from parts
                var scheme = address.Get<string>("scheme") ?? Constants.HttpScheme;
                var host = address.Get<string>("host") ?? "localhost";
                var port = address.Get<string>("port") ?? "5000";
                var path = address.Get<string>("path") ?? string.Empty;

                Prefix prefix = Prefix.Create(scheme, host, port, path);
                _listener.UriPrefixes.Add(prefix);
            }

            _listener.Start();

            ActivateRequestProcessingLimits();
        }

        /// <summary>
        /// These are merged as one operation because they should be swapped out atomically.
        /// This controls how many requests the server attempts to process concurrently.
        /// </summary>
        /// <param name="maxAccepts">The maximum number of pending accepts.</param>
        public void SetRequestProcessingLimits(int maxAccepts)
        {
            _pumpLimits = new PumpLimits(maxAccepts);

            if (_listener.IsListening)
            {
                ActivateRequestProcessingLimits();
            }
        }

        private void ActivateRequestProcessingLimits()
        {
            for (int i = _acceptorCounts; i < _pumpLimits.MaxOutstandingAccepts; i++)
            {
                ProcessRequestsWorker();
            }
        }

        /// <summary>
        /// Gets the request processing limits.
        /// </summary>
        /// <param name="maxAccepts">The maximum number of pending accepts.</param>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#", Justification = "By design")]
        public void GetRequestProcessingLimits(out int maxAccepts)
        {
            PumpLimits limits = _pumpLimits;
            maxAccepts = limits.MaxOutstandingAccepts;
        }

        // The message pump.
        // When we start listening for the next request on one thread, we may need to be sure that the
        // completion continues on another thread as to not block the current request processing.
        // The awaits will manage stack depth for us.
        private async void ProcessRequestsWorker()
        {
            int workerIndex = Interlocked.Increment(ref _acceptorCounts);
            while (_listener.IsListening && workerIndex <= _pumpLimits.MaxOutstandingAccepts)
            {
                // Receive a request
                RequestContext requestContext;
                try
                {
                    requestContext = await _listener.GetContextAsync().SupressContext();
                }
                catch (Exception exception)
                {
                    LogHelper.LogException(_logger, "ListenForNextRequestAsync", exception);
                    Contract.Assert(!_listener.IsListening);
                    return;
                }
                try
                {
                    Task.Factory.StartNew(_processRequest, requestContext);
                }
                catch (Exception ex)
                {
                    // Request processing failed to be queued in threadpool
                    // Log the error message, release throttle and move on
                    LogHelper.LogException(_logger, "ProcessRequestAsync", ex);
                }
            }
            Interlocked.Decrement(ref _acceptorCounts);
        }

        private async void ProcessRequestAsync(object requestContextObj)
        {
            var requestContext = requestContextObj as RequestContext;
            try
            {
                try
                {
                    FeatureContext featureContext = new FeatureContext(requestContext);
                    await _appFunc(featureContext.Features).SupressContext();
                    await requestContext.ProcessResponseAsync().SupressContext();
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(_logger, "ProcessRequestAsync", ex);
                    if (requestContext.Response.SentHeaders)
                    {
                        requestContext.Abort();
                    }
                    else
                    {
                        // We haven't sent a response yet, try to send a 500 Internal Server Error
                        requestContext.SetFatalResponse();
                    }
                }
                requestContext.Dispose();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(_logger, "ProcessRequestAsync", ex);
                requestContext.Abort();
                requestContext.Dispose();
            }
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
