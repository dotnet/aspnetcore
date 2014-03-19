using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Logging;

namespace Microsoft.AspNet.Server.WebListener
{
    using AppFunc = Func<object, Task>;

    public class WebListenerWrapper : IServerInformation, IDisposable
    {
        private static readonly int DefaultMaxAccepts = 5 * Environment.ProcessorCount;

        private readonly OwinWebListener _listener;
        private readonly ILogger _logger;

        private AppFunc _appFunc;

        private PumpLimits _pumpLimits;
        private int _acceptorCounts;
        private Action<object> _processRequest;

        // TODO: private IDictionary<string, object> _capabilities;

        internal WebListenerWrapper(OwinWebListener listener, ILoggerFactory loggerFactory)
        {
            Contract.Assert(listener != null);
            _listener = listener;
            _logger = LogHelper.CreateLogger(loggerFactory, typeof(WebListenerWrapper));

            _processRequest = new Action<object>(ProcessRequestAsync);
            _pumpLimits = new PumpLimits(DefaultMaxAccepts);
        }

        public OwinWebListener Listener
        {
            get { return _listener; }
        }

        // Microsoft.AspNet.Server.WebListener
        public string Name
        {
            get { return System.Reflection.IntrospectionExtensions.GetTypeInfo(GetType()).Assembly.GetName().Name; }
        }

        internal void Start(AppFunc app)
        {
            // Can't call Start twice
            Contract.Assert(_appFunc == null);

            Contract.Assert(app != null);

            _appFunc = app;

            if (_listener.UriPrefixes.Count == 0)
            {
                throw new InvalidOperationException("No address prefixes were defined.");
            }

            LogHelper.LogInfo(_logger, "Start");

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
