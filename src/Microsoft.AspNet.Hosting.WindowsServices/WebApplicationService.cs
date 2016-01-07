// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ServiceProcess;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Hosting.WindowsServices
{
    /// <summary>
    ///     Provides an implementation of a Windows service that hosts ASP.NET.
    /// </summary>
    public class WebApplicationService : ServiceBase
    {
        private IWebApplication _application;
        private bool _stopRequestedByWindows;

        /// <summary>
        /// Creates an instance of <c>WebApplicationService</c> which hosts the specified web application.
        /// </summary>
        /// <param name="application">The web application to host in the Windows service.</param>
        public WebApplicationService(IWebApplication application)
        {
            _application = application;
        }

        protected sealed override void OnStart(string[] args)
        {
            OnStarting(args);

            _application
                .Services
                .GetRequiredService<IApplicationLifetime>()
                .ApplicationStopped
                .Register(() =>
                {
                    if (!_stopRequestedByWindows)
                    {
                        Stop();
                    }
                });

            _application.Start();

            OnStarted();
        }

        protected sealed override void OnStop()
        {
            _stopRequestedByWindows = true;
            OnStopping();
            _application?.Dispose();
            OnStopped();
        }

        /// <summary>
        /// Executes before ASP.NET starts.
        /// </summary>
        /// <param name="args">The command line arguments passed to the service.</param>
        protected virtual void OnStarting(string[] args) { }

        /// <summary>
        /// Executes after ASP.NET starts.
        /// </summary>
        protected virtual void OnStarted() { }

        /// <summary>
        /// Executes before ASP.NET shuts down.
        /// </summary>
        protected virtual void OnStopping() { }

        /// <summary>
        /// Executes after ASP.NET shuts down.
        /// </summary>
        protected virtual void OnStopped() { }
    }
}
