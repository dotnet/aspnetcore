// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public class IISTestSiteFixture : IDisposable
{
    private ApplicationDeployer _deployer;
    private ILoggerFactory _loggerFactory;
    private ForwardingProvider _forwardingProvider;
    private IISDeploymentResult _deploymentResult;
    private readonly Action<IISDeploymentParameters> _configure;

    public IISTestSiteFixture() : this(_ => { })
    {
    }

    public IISDeploymentParameters DeploymentParameters { get; }

    internal IISTestSiteFixture(Action<IISDeploymentParameters> configure)
    {
        var logging = AssemblyTestLog.ForAssembly(typeof(IISTestSiteFixture).Assembly);
        _loggerFactory = logging.CreateLoggerFactory(null, nameof(IISTestSiteFixture));

        _forwardingProvider = new ForwardingProvider();
        _loggerFactory.AddProvider(_forwardingProvider);

        _configure = configure;

        DeploymentParameters = new IISDeploymentParameters()
        {
            RuntimeArchitecture = RuntimeArchitecture.x64,
            RuntimeFlavor = RuntimeFlavor.CoreClr,
            TargetFramework = Tfm.Default,
            HostingModel = HostingModel.InProcess,
            PublishApplicationBeforeDeployment = true,
            ApplicationPublisher = new PublishedApplicationPublisher(Helpers.GetInProcessTestSitesName()),
            ServerType = DeployerSelector.ServerType
        };
    }

    public HttpClient Client => DeploymentResult.HttpClient;
    public IISDeploymentResult DeploymentResult
    {
        get
        {
            EnsureInitialized();
            return _deploymentResult;
        }
    }

    public TestConnection CreateTestConnection()
    {
        return new TestConnection(Client.BaseAddress.Port);
    }

    public void Dispose()
    {
        _deploymentResult?.Dispose();
        _deployer?.Dispose();
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

    private void EnsureInitialized()
    {
        if (_deployer != null)
        {
            return;
        }

        _configure(DeploymentParameters);

        _deployer = IISApplicationDeployerFactory.Create(DeploymentParameters, _loggerFactory);
        _deploymentResult = (IISDeploymentResult)_deployer.DeployAsync().Result;
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
