// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal
{
    public class DefaultHubDispatcherTests
    {
        private class MockHubConnectionContext<TValue> : HubConnectionContext
        {
            public TaskCompletionSource<object> ReceivedCompleted = new TaskCompletionSource<object>();
            public List<TValue> Values = new List<TValue>();

            public MockHubConnectionContext(ConnectionContext connectionContext, HubConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
                : base(connectionContext, contextOptions, loggerFactory) { }

            public override ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken)
            {
                if (message is StreamItemMessage streamItemMessage)
                    Values.Add((TValue)streamItemMessage.Item);

                else if (message is CompletionMessage completionMessage)
                {
                    ReceivedCompleted.TrySetResult(null);

                    if (!string.IsNullOrEmpty(completionMessage.Error))
                    {
                        throw new Exception("Error invoking hub method: " + completionMessage.Error);
                    }
                }

                else throw new NotImplementedException();

                return default;
            }
        }

        private static DefaultHubDispatcher<THub> CreateDispatcher<THub>() where THub : Hub
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.TryAddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));
            var provider = serviceCollection.BuildServiceProvider();
            var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();

            return new DefaultHubDispatcher<THub>(
                serviceScopeFactory,
                new HubContext<THub>(new DefaultHubLifetimeManager<THub>(NullLogger<DefaultHubLifetimeManager<THub>>.Instance)),
                Options.Create(new HubOptions<THub>()),
                Options.Create(new HubOptions()),
                new Logger<DefaultHubDispatcher<THub>>(NullLoggerFactory.Instance));
        }

        private static MockHubConnectionContext<TValue> CreateConnectionContext<TValue>()
        {
            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
            var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Application, pair.Transport);
            var contextOptions = new HubConnectionContextOptions() { KeepAliveInterval = TimeSpan.Zero };

            return new MockHubConnectionContext<TValue>(
                connection,
                contextOptions,
                NullLoggerFactory.Instance);
        }

        /// <summary>
        /// For <see cref="DispatchesDerivedArguments"/>.
        /// </summary>
        private interface ITestDerivedParameter
        {
            public string Value { get; }
        }

        /// <summary>
        /// For <see cref="DispatchesDerivedArguments"/>.
        /// </summary>
        private abstract class TestDerivedParameterBase
        {
            public TestDerivedParameterBase(string value) => Value = value;
            public string Value { get; }
        }

        /// <summary>
        /// For <see cref="DispatchesDerivedArguments"/>.
        /// </summary>
        private class TestDerivedParameter : TestDerivedParameterBase, ITestDerivedParameter
        {
            public TestDerivedParameter(string value) : base(value) { }
        }

        /// <summary>
        /// For <see cref="DispatchesDerivedArguments"/>.
        /// </summary>
        private class TestDerivedParameterHub : Hub
        {
            public async IAsyncEnumerable<string> TestSubclass(TestDerivedParameterBase param, [EnumeratorCancellation]CancellationToken token)
            {
                await Task.Yield();
                yield return param.Value;
            }

            public async IAsyncEnumerable<string> TestImplementation(ITestDerivedParameter param, [EnumeratorCancellation]CancellationToken token)
            {
                await Task.Yield();
                yield return param.Value;
            }
        }

        /// <summary>
        /// Hub methods might be written by users in a way that accepts an interface or base class as a parameter
        /// and deserialization could supply a derived class (e.g. Json.NET's TypeNameHandling = TypeNameHandling.All).
        /// This test ensures implementation and subclass arguments are correctly bound for dispatch.
        /// </summary>
        [Theory]
        [InlineData(nameof(TestDerivedParameterHub.TestImplementation))]
        [InlineData(nameof(TestDerivedParameterHub.TestSubclass))]
        public async Task DispatchesDerivedArguments(string methodName)
        {
            var message = new TestDerivedParameter("Yup");
            var connectionContext = CreateConnectionContext<string>();
            var dispatcher = CreateDispatcher<TestDerivedParameterHub>();

            await dispatcher.DispatchMessageAsync(connectionContext, new StreamInvocationMessage("123", methodName, new[] { message }));
            await (connectionContext as MockHubConnectionContext<string>).ReceivedCompleted.Task;

            Assert.Single(connectionContext.Values, message.Value);
        }
    }
}
