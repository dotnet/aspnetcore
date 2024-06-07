// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class NativeAotTests : FunctionalTestBase
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void CanCallAsyncMethods()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", "false");
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.SignalR.Hub.CustomAwaitableSupport", "false");
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", "false");

        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            //System.Diagnostics.Debugger.Launch();
            var loggerFactory = new StringLoggerFactory();
            await using (var server = await InProcessTestServer<Startup>.StartServer(loggerFactory))
            {
                var hubConnectionBuilder = new HubConnectionBuilder()
                    .WithUrl(server.Url + "/hub");
                hubConnectionBuilder.Services.Configure<JsonHubProtocolOptions>(o =>
                {
                    o.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
                });
                var connection = hubConnectionBuilder.Build();

                await connection.StartAsync().DefaultTimeout();

                await connection.InvokeAsync("TaskMethod").DefaultTimeout();
                Assert.Contains("TaskMethod called", loggerFactory.ToString());

                await connection.InvokeAsync("ValueTaskMethod").DefaultTimeout();
                Assert.Contains("ValueTaskMethod called", loggerFactory.ToString());

                await connection.InvokeAsync("CustomTaskMethod").DefaultTimeout();
                Assert.Contains("CustomTaskMethod called", loggerFactory.ToString());

                var result = await connection.InvokeAsync<int>("TaskValueMethod").DefaultTimeout();
                Assert.Equal(42, result);

                result = await connection.InvokeAsync<int>("ValueTaskValueMethod").DefaultTimeout();
                Assert.Equal(43, result);

                result = await connection.InvokeAsync<int>("CustomTaskValueMethod").DefaultTimeout();
                Assert.Equal(44, result);
            }
        }, options);
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConnections();
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            services.Configure<JsonHubProtocolOptions>(o =>
            {
                o.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<AsyncMethodHub>("/hub");
            });
        }
    }

    public class AsyncMethodHub : TestHub
    {
        public async Task TaskMethod(ILogger<AsyncMethodHub> logger)
        {
            await Task.Yield(); // ensure the returned Task gets awaited correctly
            logger.LogInformation("TaskMethod called");
        }

        public async ValueTask ValueTaskMethod(ILogger<AsyncMethodHub> logger)
        {
            await Task.Yield(); // ensure the returned Task gets awaited correctly
            logger.LogInformation("ValueTaskMethod called");
        }

        public TaskDerivedType CustomTaskMethod(ILogger<AsyncMethodHub> logger)
        {
            var task = new TaskDerivedType();
            task.Start();
            logger.LogInformation("CustomTaskMethod called");
            return task;
        }

        public async Task<int> TaskValueMethod()
        {
            await Task.Yield(); // ensure the returned Task gets awaited correctly
            return 42;
        }

        public async ValueTask<int> ValueTaskValueMethod()
        {
            await Task.Yield(); // ensure the returned Task gets awaited correctly
            return 43;
        }

        public TaskOfTDerivedType<int> CustomTaskValueMethod()
        {
            var task = new TaskOfTDerivedType<int>(44);
            task.Start();
            return task;
        }
    }

    public class TaskDerivedType : Task
    {
        public TaskDerivedType()
            : base(() => { })
        {
        }
    }

    public class TaskOfTDerivedType<T> : Task<T>
    {
        public TaskOfTDerivedType(T input)
            : base(() => input)
        {
        }
    }

    public class StringLoggerFactory : ILoggerFactory
    {
        private readonly StringBuilder _log = new StringBuilder();

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string name)
        {
            return new StringLogger(name, this);
        }

        public void Dispose() { }

        public override string ToString()
        {
            return _log.ToString();
        }

        private sealed class StringLogger : ILogger
        {
            private readonly StringLoggerFactory _factory;
            private readonly string _name;

            public StringLogger(string name, StringLoggerFactory factory)
            {
                _name = name;
                _factory = factory;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return new DummyDisposable();
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Provider: {0}" + Environment.NewLine +
                    "Log level: {1}" + Environment.NewLine +
                    "Event id: {2}" + Environment.NewLine +
                    "Exception: {3}" + Environment.NewLine +
                    "Message: {4}", _name, logLevel, eventId, exception?.ToString(), formatter(state, exception));
                _factory._log.AppendLine(message);
            }

            private sealed class DummyDisposable : IDisposable
            {
                public void Dispose()
                {
                    // no-op
                }
            }
        }
    }

    [JsonSerializable(typeof(int))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
