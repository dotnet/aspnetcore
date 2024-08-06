// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests
{
    [Fact]
    public async Task HubMethodInvokesCreateActivities()
    {
        using (StartVerifiableLog())
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var testSource = new ActivitySource("test_source");
            var hubMethodTestSource = new TestActivitySource() { ActivitySource = new ActivitySource("test_custom") };

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(hubMethodTestSource);

                // Provided by hosting layer normally
                builder.AddSingleton(testSource);
            }, LoggerFactory);
            var signalrSource = serviceProvider.GetRequiredService<SignalRServerActivitySource>().ActivitySource;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource))
                    || ReferenceEquals(activitySource, hubMethodTestSource.ActivitySource) || ReferenceEquals(activitySource, signalrSource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => serverChannel.Writer.TryWrite(a)
            };
            ActivitySource.AddActivityListener(listener);

            var mockHttpRequestActivity = new Activity("HttpRequest");
            mockHttpRequestActivity.Start();
            Activity.Current = mockHttpRequestActivity;

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var connectActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<MethodHub>(connectActivity, mockHttpRequestActivity, nameof(MethodHub.OnConnectedAsync), linkedActivity: null, activityName: SignalRServerActivitySource.OnConnected);

                await client.SendInvocationAsync(nameof(MethodHub.Echo), "test").DefaultTimeout();

                var completionMessage = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                var res = (string)completionMessage.Result;
                Assert.Equal("test", res);

                var invocation1Activity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<MethodHub>(invocation1Activity, parent: null, nameof(MethodHub.Echo), mockHttpRequestActivity);

                await client.SendInvocationAsync("RenamedMethod").DefaultTimeout();
                Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());

                var invocation2Activity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<MethodHub>(invocation2Activity, parent: null, "RenamedMethod", mockHttpRequestActivity);

                await client.SendInvocationAsync(nameof(MethodHub.ActivityMethod)).DefaultTimeout();
                Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());

                var invocation3Activity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<MethodHub>(invocation3Activity, parent: null, nameof(MethodHub.ActivityMethod), mockHttpRequestActivity);

                var userCodeActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                Assert.Equal("inner", userCodeActivity.OperationName);
                Assert.Equal(invocation3Activity, userCodeActivity.Parent);

                client.Dispose();

                await connectionHandlerTask;
            }

            var disconnectActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
            AssertHubMethodActivity<MethodHub>(disconnectActivity, mockHttpRequestActivity, nameof(MethodHub.OnDisconnectedAsync), linkedActivity: null, activityName: SignalRServerActivitySource.OnDisconnected);
        }
    }

    [Fact]
    public async Task HubMethodInvokesCreateActivities_ReadTraceHeaders()
    {
        using (StartVerifiableLog())
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var testSource = new ActivitySource("test_source");
            var hubMethodTestSource = new TestActivitySource() { ActivitySource = new ActivitySource("test_custom") };

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                builder.AddSingleton(hubMethodTestSource);

                // Provided by hosting layer normally
                builder.AddSingleton(testSource);
            }, LoggerFactory);
            var signalrSource = serviceProvider.GetRequiredService<SignalRServerActivitySource>().ActivitySource;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource))
                    || ReferenceEquals(activitySource, hubMethodTestSource.ActivitySource) || ReferenceEquals(activitySource, signalrSource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => serverChannel.Writer.TryWrite(a)
            };
            ActivitySource.AddActivityListener(listener);

            var mockHttpRequestActivity = new Activity("HttpRequest");
            mockHttpRequestActivity.Start();
            Activity.Current = mockHttpRequestActivity;

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var connectActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<MethodHub>(connectActivity, mockHttpRequestActivity, nameof(MethodHub.OnConnectedAsync), linkedActivity: null, activityName: SignalRServerActivitySource.OnConnected);

                var headers = new Dictionary<string, string>
                {
                    {"traceparent", "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01"},
                    {"tracestate", "TraceState1"},
                    {"baggage", "Key1=value1, Key2=value2"}
                };

                await client.SendInvocationAsync(nameof(MethodHub.Echo), headers, "test").DefaultTimeout();

                var completionMessage = Assert.IsType<CompletionMessage>(await client.ReadAsync().DefaultTimeout());
                var res = (string)completionMessage.Result;
                Assert.Equal("test", res);

                var invocationActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<MethodHub>(invocationActivity, parent: null, nameof(MethodHub.Echo), mockHttpRequestActivity);

                Assert.True(invocationActivity.HasRemoteParent);
                Assert.Equal(ActivityIdFormat.W3C, invocationActivity.IdFormat);
                Assert.Equal("0123456789abcdef0123456789abcdef", invocationActivity.TraceId.ToHexString());
                Assert.Equal("0123456789abcdef", invocationActivity.ParentSpanId.ToHexString());
                Assert.Equal("TraceState1", invocationActivity.TraceStateString);

                client.Dispose();

                await connectionHandlerTask;
            }

            var disconnectActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
            AssertHubMethodActivity<MethodHub>(disconnectActivity, mockHttpRequestActivity, nameof(MethodHub.OnDisconnectedAsync), linkedActivity: null, activityName: SignalRServerActivitySource.OnDisconnected);
        }
    }

    [Fact]
    public async Task StreamingHubMethodCreatesActivities()
    {
        using (StartVerifiableLog())
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var testSource = new ActivitySource("test_source");

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                // Provided by hosting layer normally
                builder.AddSingleton(testSource);
            }, LoggerFactory);
            var signalrSource = serviceProvider.GetRequiredService<SignalRServerActivitySource>().ActivitySource;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource))
                    || ReferenceEquals(activitySource, signalrSource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => serverChannel.Writer.TryWrite(a)
            };
            ActivitySource.AddActivityListener(listener);

            var mockHttpRequestActivity = new Activity("HttpRequest");
            mockHttpRequestActivity.Start();
            Activity.Current = mockHttpRequestActivity;

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();
            Mock<IInvocationBinder> invocationBinder = new Mock<IInvocationBinder>();
            invocationBinder.Setup(b => b.GetStreamItemType(It.IsAny<string>())).Returns(typeof(int));

            using (var client = new TestClient(invocationBinder: invocationBinder.Object))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var connectActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<StreamingHub>(connectActivity, mockHttpRequestActivity, nameof(StreamingHub.OnConnectedAsync), linkedActivity: null, activityName: SignalRServerActivitySource.OnConnected);

                _ = await client.StreamAsync(nameof(StreamingHub.CounterChannel), 3).DefaultTimeout();

                var invocation1Activity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<StreamingHub>(invocation1Activity, parent: null, nameof(StreamingHub.CounterChannel), mockHttpRequestActivity);

                _ = await client.StreamAsync("RenamedCounterChannel", 3).DefaultTimeout();

                var invocation2Activity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<StreamingHub>(invocation2Activity, parent: null, "RenamedCounterChannel", mockHttpRequestActivity);

                client.Dispose();

                await connectionHandlerTask;
            }

            var disconnectedActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
            AssertHubMethodActivity<StreamingHub>(disconnectedActivity, mockHttpRequestActivity, nameof(StreamingHub.OnDisconnectedAsync), linkedActivity: null, activityName: SignalRServerActivitySource.OnDisconnected);
        }
    }

    [Fact]
    public async Task StreamingHubMethodCreatesActivities_ReadTraceHeaders()
    {
        using (StartVerifiableLog())
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var testSource = new ActivitySource("test_source");

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                // Provided by hosting layer normally
                builder.AddSingleton(testSource);
            }, LoggerFactory);
            var signalrSource = serviceProvider.GetRequiredService<SignalRServerActivitySource>().ActivitySource;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource))
                    || ReferenceEquals(activitySource, signalrSource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => serverChannel.Writer.TryWrite(a)
            };
            ActivitySource.AddActivityListener(listener);

            var mockHttpRequestActivity = new Activity("HttpRequest");
            mockHttpRequestActivity.Start();
            Activity.Current = mockHttpRequestActivity;

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();
            Mock<IInvocationBinder> invocationBinder = new Mock<IInvocationBinder>();
            invocationBinder.Setup(b => b.GetStreamItemType(It.IsAny<string>())).Returns(typeof(int));

            using (var client = new TestClient(invocationBinder: invocationBinder.Object))
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var connectActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<StreamingHub>(connectActivity, mockHttpRequestActivity, nameof(StreamingHub.OnConnectedAsync), linkedActivity: null, activityName: SignalRServerActivitySource.OnConnected);

                var headers = new Dictionary<string, string>
                {
                    {"traceparent", "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01"},
                    {"tracestate", "TraceState1"},
                    {"baggage", "Key1=value1, Key2=value2"}
                };

                _ = await client.StreamAsync(nameof(StreamingHub.CounterChannel), streamIds: null, headers: headers, 3).DefaultTimeout();

                var invocationActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<StreamingHub>(invocationActivity, parent: null, nameof(StreamingHub.CounterChannel), mockHttpRequestActivity);

                Assert.True(invocationActivity.HasRemoteParent);
                Assert.Equal(ActivityIdFormat.W3C, invocationActivity.IdFormat);
                Assert.Equal("0123456789abcdef0123456789abcdef", invocationActivity.TraceId.ToHexString());
                Assert.Equal("0123456789abcdef", invocationActivity.ParentSpanId.ToHexString());
                Assert.Equal("TraceState1", invocationActivity.TraceStateString);

                client.Dispose();

                await connectionHandlerTask;
            }

            var disconnectActivity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
            AssertHubMethodActivity<StreamingHub>(disconnectActivity, mockHttpRequestActivity, nameof(StreamingHub.OnDisconnectedAsync), linkedActivity: null, activityName: SignalRServerActivitySource.OnDisconnected);
        }
    }

    [Fact]
    public async Task ExceptionInOnConnectedAsyncSetsActivityErrorState()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.HubConnectionHandler" &&
                   writeContext.EventId.Name == "ErrorDispatchingHubEvent";
        }

        using (StartVerifiableLog(ExpectedErrors))
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var testSource = new ActivitySource("test_source");

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                // Provided by hosting layer normally
                builder.AddSingleton(testSource);
            }, LoggerFactory);
            var signalrSource = serviceProvider.GetRequiredService<SignalRServerActivitySource>().ActivitySource;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource))
                    || ReferenceEquals(activitySource, signalrSource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => serverChannel.Writer.TryWrite(a)
            };
            ActivitySource.AddActivityListener(listener);

            var mockHttpRequestActivity = new Activity("HttpRequest");
            mockHttpRequestActivity.Start();
            Activity.Current = mockHttpRequestActivity;

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnConnectedThrowsHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                var activity = await serverChannel.Reader.ReadAsync().DefaultTimeout();
                AssertHubMethodActivity<OnConnectedThrowsHub>(activity, mockHttpRequestActivity, nameof(OnConnectedThrowsHub.OnConnectedAsync),
                    linkedActivity: null, exceptionType: typeof(InvalidOperationException), activityName: SignalRServerActivitySource.OnConnected);
            }
        }
    }

    [Fact]
    public async Task ExceptionInOnDisconnectedAsyncSetsActivityErrorState()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.HubConnectionHandler" &&
                   writeContext.EventId.Name == "ErrorDispatchingHubEvent";
        }

        using (StartVerifiableLog(ExpectedErrors))
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var testSource = new ActivitySource("test_source");

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                // Provided by hosting layer normally
                builder.AddSingleton(testSource);
            }, LoggerFactory);
            var signalrSource = serviceProvider.GetRequiredService<SignalRServerActivitySource>().ActivitySource;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource))
                    || ReferenceEquals(activitySource, signalrSource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => serverChannel.Writer.TryWrite(a)
            };
            ActivitySource.AddActivityListener(listener);

            var mockHttpRequestActivity = new Activity("HttpRequest");
            mockHttpRequestActivity.Start();
            Activity.Current = mockHttpRequestActivity;

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnDisconnectedThrowsHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();
                client.Dispose();

                await connectionHandlerTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            var activities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 2);

            var activity = activities[1];
            AssertHubMethodActivity<OnDisconnectedThrowsHub>(activity, mockHttpRequestActivity, nameof(OnDisconnectedThrowsHub.OnDisconnectedAsync),
                linkedActivity: null, exceptionType: typeof(InvalidOperationException), activityName: SignalRServerActivitySource.OnDisconnected);
        }
    }

    [Fact]
    public async Task ExceptionInStreamingMethodSetsActivityErrorState()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                   writeContext.EventId.Name == "FailedStreaming";
        }

        using (StartVerifiableLog(ExpectedErrors))
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var testSource = new ActivitySource("test_source");

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                // Provided by hosting layer normally
                builder.AddSingleton(testSource);
            }, LoggerFactory);
            var signalrSource = serviceProvider.GetRequiredService<SignalRServerActivitySource>().ActivitySource;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource))
                    || ReferenceEquals(activitySource, signalrSource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => serverChannel.Writer.TryWrite(a)
            };
            ActivitySource.AddActivityListener(listener);

            var mockHttpRequestActivity = new Activity("HttpRequest");
            mockHttpRequestActivity.Start();
            Activity.Current = mockHttpRequestActivity;

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<StreamingHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                _ = await client.StreamAsync(nameof(StreamingHub.ExceptionAsyncEnumerable)).DefaultTimeout();

                var activities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 2);

                var activity = activities[1];
                AssertHubMethodActivity<StreamingHub>(activity, parent: null, nameof(StreamingHub.ExceptionAsyncEnumerable),
                    mockHttpRequestActivity, exceptionType: typeof(Exception));
            }
        }
    }

    [Fact]
    public async Task ExceptionInHubMethodSetsActivityErrorState()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                   writeContext.EventId.Name == "FailedInvokingHubMethod";
        }

        using (StartVerifiableLog(ExpectedErrors))
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var testSource = new ActivitySource("test_source");

            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(builder =>
            {
                // Provided by hosting layer normally
                builder.AddSingleton(testSource);
            }, LoggerFactory);
            var signalrSource = serviceProvider.GetRequiredService<SignalRServerActivitySource>().ActivitySource;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => (ReferenceEquals(activitySource, testSource))
                    || ReferenceEquals(activitySource, signalrSource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => serverChannel.Writer.TryWrite(a)
            };
            ActivitySource.AddActivityListener(listener);

            var mockHttpRequestActivity = new Activity("HttpRequest");
            mockHttpRequestActivity.Start();
            Activity.Current = mockHttpRequestActivity;

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler).DefaultTimeout();

                _ = await client.InvokeAsync(nameof(MethodHub.MethodThatThrows)).DefaultTimeout();

                var activities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 2);

                var activity = activities[1];
                AssertHubMethodActivity<MethodHub>(activity, parent: null, nameof(MethodHub.MethodThatThrows),
                    mockHttpRequestActivity, exceptionType: typeof(InvalidOperationException));
            }
        }
    }

    private static void AssertHubMethodActivity<THub>(Activity activity, Activity parent, string methodName, Activity linkedActivity, Type exceptionType = null, string activityName = null)
    {
        Assert.Equal(parent, activity.Parent);
        Assert.True(activity.IsStopped);
        Assert.Equal(SignalRServerActivitySource.Name, activity.Source.Name);
        Assert.Equal(activityName ?? SignalRServerActivitySource.InvocationIn, activity.OperationName);
        Assert.Equal($"{typeof(THub).FullName}/{methodName}", activity.DisplayName);

        var tags = activity.Tags.ToArray();
        if (exceptionType is not null)
        {
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Equal(4, tags.Length);
            Assert.Equal("error.type", tags[3].Key);
            Assert.Equal(exceptionType.FullName, tags[3].Value);
        }
        else
        {
            Assert.Equal(ActivityStatusCode.Unset, activity.Status);
            Assert.Equal(3, tags.Length);
        }

        Assert.Equal("rpc.method", tags[0].Key);
        Assert.Equal(methodName, tags[0].Value);
        Assert.Equal("rpc.system", tags[1].Key);
        Assert.Equal("signalr", tags[1].Value);
        Assert.Equal("rpc.service", tags[2].Key);
        Assert.Equal(typeof(THub).FullName, tags[2].Value);

        // Linked to original http request span
        if (linkedActivity != null)
        {
            Assert.Equal(linkedActivity.SpanId, Assert.Single(activity.Links).Context.SpanId);
        }
    }
}
