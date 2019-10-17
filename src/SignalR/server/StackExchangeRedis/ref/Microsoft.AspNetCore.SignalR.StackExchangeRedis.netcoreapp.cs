// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis
{
    public partial class RedisHubLifetimeManager<THub> : Microsoft.AspNetCore.SignalR.HubLifetimeManager<THub>, System.IDisposable where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        public RedisHubLifetimeManager(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.SignalR.StackExchangeRedis.RedisHubLifetimeManager<THub>> logger, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.SignalR.StackExchangeRedis.RedisOptions> options, Microsoft.AspNetCore.SignalR.IHubProtocolResolver hubProtocolResolver) { }
        public override System.Threading.Tasks.Task AddToGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Dispose() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task OnConnectedAsync(Microsoft.AspNetCore.SignalR.HubConnectionContext connection) { throw null; }
        public override System.Threading.Tasks.Task OnDisconnectedAsync(Microsoft.AspNetCore.SignalR.HubConnectionContext connection) { throw null; }
        public override System.Threading.Tasks.Task RemoveFromGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendAllAsync(string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendAllExceptAsync(string methodName, object[] args, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendConnectionAsync(string connectionId, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendConnectionsAsync(System.Collections.Generic.IReadOnlyList<string> connectionIds, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendGroupAsync(string groupName, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendGroupExceptAsync(string groupName, string methodName, object[] args, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendGroupsAsync(System.Collections.Generic.IReadOnlyList<string> groupNames, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendUserAsync(string userId, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task SendUsersAsync(System.Collections.Generic.IReadOnlyList<string> userIds, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class RedisOptions
    {
        public RedisOptions() { }
        public StackExchange.Redis.ConfigurationOptions Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<System.IO.TextWriter, System.Threading.Tasks.Task<StackExchange.Redis.IConnectionMultiplexer>> ConnectionFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class StackExchangeRedisDependencyInjectionExtensions
    {
        public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddStackExchangeRedis(this Microsoft.AspNetCore.SignalR.ISignalRServerBuilder signalrBuilder) { throw null; }
        public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddStackExchangeRedis(this Microsoft.AspNetCore.SignalR.ISignalRServerBuilder signalrBuilder, System.Action<Microsoft.AspNetCore.SignalR.StackExchangeRedis.RedisOptions> configure) { throw null; }
        public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddStackExchangeRedis(this Microsoft.AspNetCore.SignalR.ISignalRServerBuilder signalrBuilder, string redisConnectionString) { throw null; }
        public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddStackExchangeRedis(this Microsoft.AspNetCore.SignalR.ISignalRServerBuilder signalrBuilder, string redisConnectionString, System.Action<Microsoft.AspNetCore.SignalR.StackExchangeRedis.RedisOptions> configure) { throw null; }
    }
}
