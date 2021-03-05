using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal sealed class RequestContext<TContext> : RequestContext where TContext : notnull
    {
        private readonly IHttpApplication<TContext> _application;
        private readonly MessagePump _messagePump;

        public RequestContext(IHttpApplication<TContext> application, MessagePump messagePump, HttpSysListener server, uint? bufferSize, ulong requestId)
            : base(server, bufferSize, requestId)
        {
            _application = application;
            _messagePump = messagePump;
        }

        protected override async Task ExecuteAsync()
        {
            var messagePump = _messagePump;
            var application = _application;

            try
            {
                InitializeFeatures();

                if (messagePump.Stopping)
                {
                    SetFatalResponse(503);
                    return;
                }

                TContext? context = default;
                messagePump.IncrementOutstandingRequest();
                try
                {
                    context = application.CreateContext(Features);
                    try
                    {
                        await application.ProcessRequestAsync(context);
                        await CompleteAsync();
                    }
                    finally
                    {
                        await OnCompleted();
                    }
                    application.DisposeContext(context, null);
                    Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogError(LoggerEventIds.RequestProcessError, ex, "ProcessRequestAsync");
                    if (context != null)
                    {
                        application.DisposeContext(context, ex);
                    }
                    if (Response.HasStarted)
                    {
                        // HTTP/2 INTERNAL_ERROR = 0x2 https://tools.ietf.org/html/rfc7540#section-7
                        // Otherwise the default is Cancel = 0x8.
                        SetResetCode(2);
                        Abort();
                    }
                    else
                    {
                        // We haven't sent a response yet, try to send a 500 Internal Server Error
                        Response.Headers.IsReadOnly = false;
                        Response.Trailers.IsReadOnly = false;
                        Response.Headers.Clear();
                        Response.Trailers.Clear();

                        if (ex is BadHttpRequestException badHttpRequestException)
                        {
                            SetFatalResponse(badHttpRequestException.StatusCode);
                        }
                        else
                        {
                            SetFatalResponse(StatusCodes.Status500InternalServerError);
                        }
                    }
                }
                finally
                {
                    if (messagePump.DecrementOutstandingRequest() == 0 && messagePump.Stopping)
                    {
                        Logger.LogInformation(LoggerEventIds.RequestsDrained, "All requests drained.");
                        messagePump.SetShutdownSignal();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(LoggerEventIds.RequestError, ex, "ProcessRequestAsync");
                Abort();
            }
        }
    }

}
