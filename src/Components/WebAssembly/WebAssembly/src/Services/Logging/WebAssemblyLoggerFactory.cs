// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    /// <summary>
    /// Produces instances of <see cref="ILogger"/> classes based on the given providers.
    /// </summary>
    public class WebAssemblyLoggerFactory : ILoggerFactory
    {
        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();
        private readonly List<ProviderRegistration> _providerRegistrations = new List<ProviderRegistration>();
        private readonly object _sync = new object();
        private volatile bool _disposed;

        public WebAssemblyLoggerFactory() : this(Enumerable.Empty<ILoggerProvider>()) { }

        /// <summary>
        /// Creates a new <see cref="WebAssemblyLoggerFactory"/> instance.
        /// </summary>
        /// <param name="providers">The providers to use in producing <see cref="ILogger"/> instances.</param>
        public WebAssemblyLoggerFactory(IEnumerable<ILoggerProvider> providers)
        {
            foreach (var provider in providers)
            {
                AddProviderRegistration(provider, dispose: false);
            }
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> with the given <paramref name="categoryName"/>.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>The <see cref="ILogger"/> that was created.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(WebAssemblyLoggerFactory));
            }

            lock (_sync)
            {
                if (!_loggers.TryGetValue(categoryName, out var logger))
                {
                    logger = new Logger
                    {
                        Loggers = CreateLoggers(categoryName),
                    };

                    _loggers[categoryName] = logger;
                }

                return logger;
            }
        }

        /// <summary>
        /// Adds the given provider to those used in creating <see cref="ILogger"/> instances.
        /// </summary>
        /// <param name="provider">The <see cref="ILoggerProvider"/> to add.</param>
        public void AddProvider(ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(WebAssemblyLoggerFactory));
            }

            lock (_sync)
            {
                AddProviderRegistration(provider, dispose: true);

                foreach (var existingLogger in _loggers)
                {
                    var logger = existingLogger.Value;
                    var WebAssemblyLoggerInformation = logger.Loggers;

                    var newLoggerIndex = WebAssemblyLoggerInformation.Length;
                    Array.Resize(ref WebAssemblyLoggerInformation, WebAssemblyLoggerInformation.Length + 1);
                    WebAssemblyLoggerInformation[newLoggerIndex] = new WebAssemblyLoggerInformation(provider, existingLogger.Key);

                    logger.Loggers = WebAssemblyLoggerInformation;
                }
            }
        }

        private void AddProviderRegistration(ILoggerProvider provider, bool dispose)
        {
            _providerRegistrations.Add(new ProviderRegistration
            {
                Provider = provider,
                ShouldDispose = dispose
            });
        }

        private WebAssemblyLoggerInformation[] CreateLoggers(string categoryName)
        {
            var loggers = new WebAssemblyLoggerInformation[_providerRegistrations.Count];
            for (var i = 0; i < _providerRegistrations.Count; i++)
            {
                loggers[i] = new WebAssemblyLoggerInformation(_providerRegistrations[i].Provider, categoryName);
            }
            return loggers;
        }

        /// <summary>
        /// Check if the factory has been disposed.
        /// </summary>
        /// <returns>True when <see cref="Dispose()"/> as been called</returns>
        protected virtual bool CheckDisposed() => _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                foreach (var registration in _providerRegistrations)
                {
                    try
                    {
                        if (registration.ShouldDispose)
                        {
                            registration.Provider.Dispose();
                        }
                    }
                    catch
                    {
                        // Swallow exceptions on dispose
                    }
                }
            }
        }

        private struct ProviderRegistration
        {
            public ILoggerProvider Provider;
            public bool ShouldDispose;
        }
    }
}
