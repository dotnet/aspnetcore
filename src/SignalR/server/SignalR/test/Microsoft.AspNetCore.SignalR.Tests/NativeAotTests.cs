// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class NativeAotTests : FunctionalTestBase
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void CanCallAsyncMethods()
    {
        RunNativeAotTest(static async () =>
        {
            //System.Diagnostics.Debugger.Launch();
            var loggerFactory = new StringLoggerFactory();
            await using (var server = await InProcessTestServer<Startup<AsyncMethodHub>>.StartServer(loggerFactory))
            {
                var hubConnectionBuilder = new HubConnectionBuilder()
                    .WithUrl(server.Url + "/hub");
                AppJsonSerializerContext.AddToJsonHubProtocol(hubConnectionBuilder.Services);
                var connection = hubConnectionBuilder.Build();

                await connection.StartAsync().DefaultTimeout();

                await connection.InvokeAsync(nameof(AsyncMethodHub.TaskMethod)).DefaultTimeout();
                Assert.Contains("TaskMethod called", loggerFactory.ToString());

                await connection.InvokeAsync(nameof(AsyncMethodHub.ValueTaskMethod)).DefaultTimeout();
                Assert.Contains("ValueTaskMethod called", loggerFactory.ToString());

                await connection.InvokeAsync(nameof(AsyncMethodHub.CustomTaskMethod)).DefaultTimeout();
                Assert.Contains("CustomTaskMethod called", loggerFactory.ToString());

                var result = await connection.InvokeAsync<int>(nameof(AsyncMethodHub.TaskValueMethod)).DefaultTimeout();
                Assert.Equal(42, result);

                result = await connection.InvokeAsync<int>(nameof(AsyncMethodHub.ValueTaskValueMethod)).DefaultTimeout();
                Assert.Equal(43, result);

                result = await connection.InvokeAsync<int>(nameof(AsyncMethodHub.CustomTaskValueMethod)).DefaultTimeout();
                Assert.Equal(44, result);

                var counterResults = new List<string>();
                await foreach (var item in connection.StreamAsync<string>(nameof(AsyncMethodHub.CounterAsyncEnumerable), 4))
                {
                    counterResults.Add(item);
                }
                Assert.Equal(["0", "1", "2", "3"], counterResults);

                counterResults.Clear();
                await foreach (var item in connection.StreamAsync<string>(nameof(AsyncMethodHub.CounterAsyncEnumerableImpl), 5))
                {
                    counterResults.Add(item);
                }
                Assert.Equal(["0", "1", "2", "3", "4"], counterResults);

                var echoResults = new List<string>();
                var asyncEnumerable = connection.StreamAsync<string>(nameof(AsyncMethodHub.StreamEchoAsyncEnumerable), StreamMessages());
                await foreach (var item in asyncEnumerable)
                {
                    echoResults.Add(item);
                }
                Assert.Equal(["echo:message one", "echo:message two"], echoResults);

                echoResults.Clear();
                var channel = Channel.CreateBounded<string>(10);
                var echoResponseReader = await connection.StreamAsChannelAsync<string>(nameof(AsyncMethodHub.StreamEcho), channel.Reader);
                await channel.Writer.WriteAsync("some data");
                await channel.Writer.WriteAsync("some more data");
                await channel.Writer.WriteAsync("even more data");
                channel.Writer.Complete();

                await foreach (var item in echoResponseReader.ReadAllAsync())
                {
                    echoResults.Add(item);
                }
                Assert.Equal(["echo:some data", "echo:some more data", "echo:even more data"], echoResults);

                var streamValueTypeResults = new List<int>();
                await foreach (var item in connection.StreamAsync<int>(nameof(AsyncMethodHub.ReturnEnumerableValueType)))
                {
                    streamValueTypeResults.Add(item);
                }
                Assert.Equal([1, 2], streamValueTypeResults);

                var returnChannelValueTypeResults = new List<char>();
                var returnChannelValueTypeReader = await connection.StreamAsChannelAsync<char>(nameof(AsyncMethodHub.ReturnChannelValueType), "Hello");
                await foreach (var item in returnChannelValueTypeReader.ReadAllAsync())
                {
                    returnChannelValueTypeResults.Add(item);
                }
                Assert.Equal(['H', 'e', 'l', 'l', 'o'], returnChannelValueTypeResults);

                // Even though SignalR server doesn't support Hub methods with streaming value types in native AOT (https://github.com/dotnet/aspnetcore/issues/56179),
                // still test that the client can send them.
                var stringResult = await connection.InvokeAsync<string>(nameof(AsyncMethodHub.EnumerableIntParameter), StreamInts());
                Assert.Equal("1, 2, 3", stringResult);

                var channelShorts = Channel.CreateBounded<short>(10);
                await channelShorts.Writer.WriteAsync(9);
                await channelShorts.Writer.WriteAsync(8);
                await channelShorts.Writer.WriteAsync(7);
                channelShorts.Writer.Complete();

                stringResult = await connection.InvokeAsync<string>(nameof(AsyncMethodHub.ChannelShortParameter), channelShorts.Reader);
                Assert.Equal("9, 8, 7", stringResult);
            }
        });
    }

    private static async IAsyncEnumerable<string> StreamMessages()
    {
        await Task.Yield();
        yield return "message one";
        await Task.Yield();
        yield return "message two";
    }

    private static async IAsyncEnumerable<int> StreamInts()
    {
        await Task.Yield();
        yield return 1;
        await Task.Yield();
        yield return 2;
        await Task.Yield();
        yield return 3;
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void UsingValueTypesInStreamingThrows()
    {
        RunNativeAotTest(static async () =>
        {
            var e = await Assert.ThrowsAsync<InvalidOperationException>(() => InProcessTestServer<Startup<ChannelValueTypeMethodHub>>.StartServer(NullLoggerFactory.Instance));
            Assert.Contains("Method 'Microsoft.AspNetCore.SignalR.Tests.NativeAotTests+ChannelValueTypeMethodHub.StreamValueType' is not supported with native AOT because it has a parameter of type 'System.Threading.Channels.ChannelReader`1[System.Double]'.", e.Message);
        });

        RunNativeAotTest(static async () =>
        {
            var e = await Assert.ThrowsAsync<InvalidOperationException>(() => InProcessTestServer<Startup<EnumerableValueTypeMethodHub>>.StartServer(NullLoggerFactory.Instance));
            Assert.Contains("Method 'Microsoft.AspNetCore.SignalR.Tests.NativeAotTests+EnumerableValueTypeMethodHub.StreamValueType' is not supported with native AOT because it has a parameter of type 'System.Collections.Generic.IAsyncEnumerable`1[System.Single]'.", e.Message);
        });
    }

    private static void RunNativeAotTest(Func<Task> test)
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", "false");
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.SignalR.Hub.IsCustomAwaitableSupported", "false");
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", "false");

        using var remoteHandle = RemoteExecutor.Invoke(test, options);
    }

    public class Startup<THub> where THub : Hub
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConnections();
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            AppJsonSerializerContext.AddToJsonHubProtocol(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<THub>("/hub");
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

        public async IAsyncEnumerable<string> CounterAsyncEnumerable(int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Yield();
                yield return i.ToString(CultureInfo.InvariantCulture);
            }
        }

        public StreamingHub.AsyncEnumerableImpl<string> CounterAsyncEnumerableImpl(int count)
        {
            return new StreamingHub.AsyncEnumerableImpl<string>(CounterAsyncEnumerable(count));
        }

        public ChannelReader<string> StreamEcho(ChannelReader<string> source)
        {
            Channel<string> output = Channel.CreateUnbounded<string>();

            _ = Task.Run(async () =>
            {
                await foreach (var item in source.ReadAllAsync())
                {
                    await output.Writer.WriteAsync("echo:" + item);
                }

                output.Writer.TryComplete();
            });

            return output.Reader;
        }

        public async IAsyncEnumerable<string> StreamEchoAsyncEnumerable(IAsyncEnumerable<string> source)
        {
            await foreach (var item in source)
            {
                yield return "echo:" + item;
            }
        }

        public async IAsyncEnumerable<int> ReturnEnumerableValueType()
        {
            await Task.Yield();
            yield return 1;
            await Task.Yield();
            yield return 2;
        }

        public ChannelReader<char> ReturnChannelValueType(string source)
        {
            Channel<char> output = Channel.CreateUnbounded<char>();

            _ = Task.Run(async () =>
            {
                foreach (var item in source)
                {
                    await Task.Yield();
                    await output.Writer.WriteAsync(item);
                }

                output.Writer.TryComplete();
            });

            return output.Reader;
        }

        // using 'object' as the streaming parameter type because streaming ValueTypes is not supported on the server
        public async Task<string> EnumerableIntParameter(IAsyncEnumerable<object> source)
        {
            var result = new StringBuilder();
            var first = true;
            // These get deserialized as JsonElement since the streaming parameter is 'object'
            await foreach (JsonElement item in source)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    result.Append(", ");
                }

                result.Append(item.GetInt32());
            }
            return result.ToString();
        }

        // using 'object' as the streaming parameter type because streaming ValueTypes is not supported on the server
        public async Task<string> ChannelShortParameter(ChannelReader<object> source)
        {
            var result = new StringBuilder();
            var first = true;
            // These get deserialized as JsonElement since the streaming parameter is 'object'
            await foreach (JsonElement item in source.ReadAllAsync())
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    result.Append(", ");
                }

                result.Append(item.GetInt16());
            }
            return result.ToString();
        }
    }

    public class ChannelValueTypeMethodHub : TestHub
    {
        public async Task StreamValueType(ILogger<ChannelValueTypeMethodHub> logger, ChannelReader<double> source)
        {
            await foreach (var item in source.ReadAllAsync())
            {
                logger.LogInformation("Received: {item}", item);
            }
        }
    }

    public class EnumerableValueTypeMethodHub : TestHub
    {
        public async Task StreamValueType(ILogger<EnumerableValueTypeMethodHub> logger, IAsyncEnumerable<float> source)
        {
            await foreach (var item in source)
            {
                logger.LogInformation("Received: {item}", item);
            }
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

    [JsonSerializable(typeof(object))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(short))]
    [JsonSerializable(typeof(char))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
        public static void AddToJsonHubProtocol(IServiceCollection services)
        {
            services.Configure<JsonHubProtocolOptions>(o =>
            {
                o.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, Default);
            });
        }
    }
}
