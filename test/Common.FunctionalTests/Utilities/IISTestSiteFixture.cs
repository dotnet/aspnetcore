// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class IISTestSiteFixture : IDisposable
    {
        private readonly ApplicationDeployer _deployer;
        private readonly ForwardingProvider _forwardingProvider;

        public IISTestSiteFixture()
        {
            var logging = AssemblyTestLog.ForAssembly(typeof(IISTestSiteFixture).Assembly);

            var deploymentParameters = new DeploymentParameters(Helpers.GetInProcessTestSitesPath(),
                DeployerSelector.ServerType,
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp22,
                AncmVersion = AncmVersion.AspNetCoreModuleV2,
                HostingModel = HostingModel.InProcess,
                PublishApplicationBeforeDeployment = true,
            };

            _forwardingProvider = new ForwardingProvider();
            var loggerFactory = logging.CreateLoggerFactory(null, nameof(IISTestSiteFixture));
            loggerFactory.AddProvider(_forwardingProvider);

            _deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory);

            DeploymentResult = _deployer.DeployAsync().Result;
            Client = DeploymentResult.HttpClient;
            BaseUri = DeploymentResult.ApplicationBaseUri;
            ShutdownToken = DeploymentResult.HostShutdownToken;
        }

        public string BaseUri { get; }
        public HttpClient Client { get; }
        public CancellationToken ShutdownToken { get; }
        public DeploymentResult DeploymentResult { get; }

        public TestConnection CreateTestConnection()
        {
            return new TestConnection(Client.BaseAddress.Port);
        }

        public void Dispose()
        {
            _deployer.Dispose();
        }

        public void Attach(LoggedTest test)
        {
            if (_forwardingProvider.LoggerFactory != null)
            {
                throw new InvalidOperationException("Test instance is already attached to this fixture");
            }

            _forwardingProvider.LoggerFactory = test.LoggerFactory;
        }

        public void Detach(LoggedTest test)
        {
            if (_forwardingProvider.LoggerFactory != test.LoggerFactory)
            {
                throw new InvalidOperationException("Different test is attached to this fixture");
            }

            _forwardingProvider.LoggerFactory = null;
        }

        private class ForwardingProvider : ILoggerProvider
        {
            private readonly List<ForwardingLogger> _loggers = new List<ForwardingLogger>();

            private ILoggerFactory _loggerFactory;

            public ILoggerFactory LoggerFactory
            {
                get => _loggerFactory;
                set
                {

                    lock (_loggers)
                    {
                        _loggerFactory = value;
                        foreach (var logger in _loggers)
                        {
                            logger.Logger = _loggerFactory?.CreateLogger("FIXTURE:" + logger.Name);
                        }
                    }
                }
            }

            public void Dispose()
            {
                lock (_loggers)
                {
                    _loggers.Clear();
                }
            }

            public ILogger CreateLogger(string categoryName)
            {
                lock (_loggers)
                {
                    var logger = new ForwardingLogger(categoryName);
                    _loggers.Add(logger);
                    return logger;
                }
            }
        }

        internal class ForwardingLogger : ILogger
        {
            public ForwardingLogger(string name)
            {
                Name = name;
            }

            public ILogger Logger { get; set; }
            public string Name { get; set; }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Logger?.Log(logLevel, eventId, state, exception, formatter);
            }

            public bool IsEnabled(LogLevel logLevel) => Logger?.IsEnabled(logLevel) == true;

            public IDisposable BeginScope<TState>(TState state) => Logger?.BeginScope(state);
        }
    }

}
