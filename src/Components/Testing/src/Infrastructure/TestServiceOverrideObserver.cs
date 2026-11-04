// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

// Subscribes to the "Microsoft.Extensions.Hosting" DiagnosticListener to
// intercept host building. When the "HostBuilding" event fires, it invokes
// the provided configureServices action AFTER the app's own ConfigureServices —
// equivalent to WebApplicationFactory.ConfigureTestServices.
//
// The action is created by TestReadinessHostingStartup from the static method
// specified via E2E_TEST_SERVICES_TYPE + E2E_TEST_SERVICES_METHOD env vars.
internal class TestServiceOverrideObserver : IObserver<DiagnosticListener>
{
    private readonly Action<IServiceCollection> _configureServices;

    internal TestServiceOverrideObserver(Action<IServiceCollection> configureServices)
    {
        _configureServices = configureServices;
    }

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "Microsoft.Extensions.Hosting")
        {
            listener.Subscribe(new HostBuildingObserver(_configureServices));
        }
    }

    public void OnError(Exception error) { }
    public void OnCompleted() { }

    private class HostBuildingObserver : IObserver<KeyValuePair<string, object?>>
    {
        private readonly Action<IServiceCollection> _configureServices;

        internal HostBuildingObserver(Action<IServiceCollection> configureServices)
        {
            _configureServices = configureServices;
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "HostBuilding" &&
                value.Value is Microsoft.Extensions.Hosting.IHostBuilder hostBuilder)
            {
                hostBuilder.ConfigureServices((_, services) =>
                {
                    _configureServices(services);
                });
            }
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}
