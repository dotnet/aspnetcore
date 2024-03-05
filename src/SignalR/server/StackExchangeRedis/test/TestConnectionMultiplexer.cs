// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using StackExchange.Redis;
using StackExchange.Redis.Maintenance;
using StackExchange.Redis.Profiling;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class TestConnectionMultiplexer : IConnectionMultiplexer
{
    public string ClientName => throw new NotImplementedException();

    public string Configuration => throw new NotImplementedException();

    public int TimeoutMilliseconds => throw new NotImplementedException();

    public long OperationCount => throw new NotImplementedException();

    public bool PreserveAsyncOrder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool IsConnected => true;

    public bool IncludeDetailInExceptions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int StormLogThreshold { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool IsConnecting => throw new NotImplementedException();

    public event EventHandler<RedisErrorEventArgs> ErrorMessage
    {
        add { }
        remove { }
    }

    public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed
    {
        add { }
        remove { }
    }

    public event EventHandler<InternalErrorEventArgs> InternalError
    {
        add { }
        remove { }
    }

    public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored
    {
        add { }
        remove { }
    }

    public event EventHandler<EndPointEventArgs> ConfigurationChanged
    {
        add { }
        remove { }
    }

    public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast
    {
        add { }
        remove { }
    }

    public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved
    {
        add { }
        remove { }
    }

    private readonly TestRedisServer _server;

    public event EventHandler<ServerMaintenanceEvent> ServerMaintenanceEvent
    {
        add { }
        remove { }
    }

    public TestConnectionMultiplexer(TestRedisServer server)
    {
        _server = server;
    }

    public void BeginProfiling(object forContext)
    {
        throw new NotImplementedException();
    }

    public void Close(bool allowCommandsToComplete = true)
    {
        throw new NotImplementedException();
    }

    public Task CloseAsync(bool allowCommandsToComplete = true)
    {
        throw new NotImplementedException();
    }

    public bool Configure(TextWriter log = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ConfigureAsync(TextWriter log = null)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ProfiledCommandEnumerable FinishProfiling(object forContext, bool allowCleanupSweep = true)
    {
        throw new NotImplementedException();
    }

    public ServerCounters GetCounters()
    {
        throw new NotImplementedException();
    }

    public IDatabase GetDatabase(int db = -1, object asyncState = null)
    {
        throw new NotImplementedException();
    }

    public EndPoint[] GetEndPoints(bool configuredOnly = false)
    {
        throw new NotImplementedException();
    }

    public IServer GetServer(string host, int port, object asyncState = null)
    {
        throw new NotImplementedException();
    }

    public IServer GetServer(string hostAndPort, object asyncState = null)
    {
        throw new NotImplementedException();
    }

    public IServer GetServer(IPAddress host, int port)
    {
        throw new NotImplementedException();
    }

    public IServer GetServer(EndPoint endpoint, object asyncState = null)
    {
        throw new NotImplementedException();
    }

    public string GetStatus()
    {
        throw new NotImplementedException();
    }

    public void GetStatus(TextWriter log)
    {
        throw new NotImplementedException();
    }

    public string GetStormLog()
    {
        throw new NotImplementedException();
    }

    public ISubscriber GetSubscriber(object asyncState = null)
    {
        return new TestSubscriber(_server);
    }

    public int HashSlot(RedisKey key)
    {
        throw new NotImplementedException();
    }

    public long PublishReconfigure(CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public void ResetStormLog()
    {
        throw new NotImplementedException();
    }

    public void Wait(Task task)
    {
        throw new NotImplementedException();
    }

    public T Wait<T>(Task<T> task)
    {
        throw new NotImplementedException();
    }

    public void WaitAll(params Task[] tasks)
    {
        throw new NotImplementedException();
    }

    public void RegisterProfiler(Func<ProfilingSession> profilingSessionProvider)
    {
        throw new NotImplementedException();
    }

    public int GetHashSlot(RedisKey key)
    {
        throw new NotImplementedException();
    }

    public void ExportConfiguration(Stream destination, ExportOptions options = (ExportOptions)(-1))
    {
        throw new NotImplementedException();
    }

    public IServer[] GetServers()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync() => default;

    public void AddLibraryNameSuffix(string suffix) { } // don't need to implement
}

public class TestRedisServer
{
    private readonly ConcurrentDictionary<RedisChannel, List<(int, Action<RedisChannel, RedisValue>)>> _subscriptions =
        new ConcurrentDictionary<RedisChannel, List<(int, Action<RedisChannel, RedisValue>)>>();

    public long Publish(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
    {
        AssertRedisChannel(channel);

        if (_subscriptions.TryGetValue(channel, out var handlers))
        {
            lock (handlers)
            {
                foreach (var (_, handler) in handlers)
                {
                    handler(channel, message);
                }
            }
        }

        return handlers != null ? handlers.Count : 0;
    }

    public void Subscribe(ChannelMessageQueue messageQueue, int subscriberId, CommandFlags flags = CommandFlags.None)
    {
        AssertRedisChannel(messageQueue.Channel);

        Action<RedisChannel, RedisValue> handler = (channel, value) =>
        {
            // Workaround for https://github.com/StackExchange/StackExchange.Redis/issues/969
            // ChannelMessageQueue isn't mockable currently, this works around that by using private reflection
            typeof(ChannelMessageQueue).GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(messageQueue, new object[] { channel, value });
        };

        _subscriptions.AddOrUpdate(messageQueue.Channel, _ => new List<(int, Action<RedisChannel, RedisValue>)> { (subscriberId, handler) }, (_, list) =>
        {
            lock (list)
            {
                list.Add((subscriberId, handler));
            }
            return list;
        });
    }

    public void Unsubscribe(RedisChannel channel, int subscriberId, CommandFlags flags = CommandFlags.None)
    {
        AssertRedisChannel(channel);

        if (_subscriptions.TryGetValue(channel, out var list))
        {
            lock (list)
            {
                list.RemoveAll((item) => item.Item1 == subscriberId);
            }
        }
    }

    internal static void AssertRedisChannel(RedisChannel channel)
    {
        Assert.False(channel.IsPattern);
    }
}

public class TestSubscriber : ISubscriber
{
    private static int StaticId;

    private readonly int _id;
    private readonly TestRedisServer _server;
    public ConnectionMultiplexer Multiplexer => throw new NotImplementedException();

    IConnectionMultiplexer IRedisAsync.Multiplexer => throw new NotImplementedException();

    public TestSubscriber(TestRedisServer server)
    {
        _server = server;
        _id = Interlocked.Increment(ref StaticId);
    }

    public EndPoint IdentifyEndpoint(RedisChannel channel, CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public Task<EndPoint> IdentifyEndpointAsync(RedisChannel channel, CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public bool IsConnected(RedisChannel channel = default)
    {
        throw new NotImplementedException();
    }

    public TimeSpan Ping(CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public long Publish(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
    {
        TestRedisServer.AssertRedisChannel(channel);

        return _server.Publish(channel, message, flags);
    }

    public async Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
    {
        TestRedisServer.AssertRedisChannel(channel);

        await Task.Yield();
        return Publish(channel, message, flags);
    }

    public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None)
    {
        TestRedisServer.AssertRedisChannel(channel);

        Subscribe(channel, handler, flags);
        return Task.CompletedTask;
    }

    public EndPoint SubscribedEndpoint(RedisChannel channel)
    {
        throw new NotImplementedException();
    }

    public bool TryWait(Task task)
    {
        throw new NotImplementedException();
    }

    public void Unsubscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler = null, CommandFlags flags = CommandFlags.None)
    {
        TestRedisServer.AssertRedisChannel(channel);

        _server.Unsubscribe(channel, _id, flags);
    }

    public void UnsubscribeAll(CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public Task UnsubscribeAllAsync(CommandFlags flags = CommandFlags.None)
    {
        throw new NotImplementedException();
    }

    public Task UnsubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler = null, CommandFlags flags = CommandFlags.None)
    {
        TestRedisServer.AssertRedisChannel(channel);

        Unsubscribe(channel, handler, flags);
        return Task.CompletedTask;
    }

    public void Wait(Task task)
    {
        throw new NotImplementedException();
    }

    public T Wait<T>(Task<T> task)
    {
        throw new NotImplementedException();
    }

    public void WaitAll(params Task[] tasks)
    {
        throw new NotImplementedException();
    }

    public ChannelMessageQueue Subscribe(RedisChannel channel, CommandFlags flags = CommandFlags.None)
    {
        TestRedisServer.AssertRedisChannel(channel);

        // Workaround for https://github.com/StackExchange/StackExchange.Redis/issues/969
        var redisSubscriberType = typeof(RedisChannel).Assembly.GetType("StackExchange.Redis.RedisSubscriber");
        var ctor = typeof(ChannelMessageQueue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new Type[] { typeof(RedisChannel).MakeByRefType(), redisSubscriberType }, modifiers: null);

        var queue = (ChannelMessageQueue)ctor.Invoke(new object[] { channel, null });
        _server.Subscribe(queue, _id);
        return queue;
    }

    public Task<ChannelMessageQueue> SubscribeAsync(RedisChannel channel, CommandFlags flags = CommandFlags.None)
    {
        TestRedisServer.AssertRedisChannel(channel);

        var t = Subscribe(channel, flags);
        return Task.FromResult(t);
    }
}
