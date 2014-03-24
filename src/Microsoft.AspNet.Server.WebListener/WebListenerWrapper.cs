using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Logging;

namespace Microsoft.AspNet.Server.WebListener
{
    using AppFunc = Func<object, Task>;

    internal class WebListenerWrapper : IDisposable
    {
        private static readonly int DefaultMaxAccepts = 5 * Environment.ProcessorCount;

        private readonly OwinWebListener _listener;
        private readonly ILogger _logger;

        private AppFunc _appFunc;

        private int _maxAccepts;
        private int _acceptorCounts;
        private Action<object> _processRequest;

        // TODO: private IDictionary<string, object> _capabilities;

        internal WebListenerWrapper(OwinWebListener listener, ILoggerFactory loggerFactory)
        {
            Contract.Assert(listener != null);
            _listener = listener;
            _logger = LogHelper.CreateLogger(loggerFactory, typeof(WebListenerWrapper));

            _processRequest = new Action<object>(ProcessRequestAsync);
            _maxAccepts = DefaultMaxAccepts;
        }

        internal OwinWebListener Listener
        {
            get { return _listener; }
        }

        internal int MaxAccepts
        {
            get
            {
                return _maxAccepts;
            }
            set
            {
                _maxAccepts = value;
                if (_listener.IsListening)
                {
                    ActivateRequestProcessingLimits();
                }
            }
        }

        internal void Start(AppFunc app)
        {
            // Can't call Start twice
            Contract.Assert(_appFunc == null);

            Contract.Assert(app != null);

            _appFunc = app;

            if (_listener.UrlPrefixes.Count == 0)
            {
                throw new InvalidOperationException("No address prefixes were defined.");
            }

            LogHelper.LogInfo(_logger, "Start");

            _listener.Start();

            ActivateRequestProcessingLimits();
        }

        private void ActivateRequestProcessingLimits()
        {
            for (int i = _acceptorCounts; i < MaxAccepts; i++)
            {
                ProcessRequestsWorker();
            }
        }

        // The message pump.
        // When we start listening for the next request on one thread, we may need to be sure that the
        // completion continues on another thread as to not block the current request processing.
        // The awaits will manage stack depth for us.
        private async void ProcessRequestsWorker()
        {
            int workerIndex = Interlocked.Increment(ref _acceptorCounts);
            while (_listener.IsListening && workerIndex <= MaxAccepts)
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
