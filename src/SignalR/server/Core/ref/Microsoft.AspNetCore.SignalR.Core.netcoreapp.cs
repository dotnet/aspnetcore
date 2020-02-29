// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    public static partial class ClientProxyExtensions
    {
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, object arg2, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, object arg1, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendAsync(this Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string method, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class DefaultHubLifetimeManager<THub> : Microsoft.AspNetCore.SignalR.HubLifetimeManager<THub> where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        public DefaultHubLifetimeManager(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.SignalR.DefaultHubLifetimeManager<THub>> logger) { }
        public override System.Threading.Tasks.Task AddToGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
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
    public partial class DefaultUserIdProvider : Microsoft.AspNetCore.SignalR.IUserIdProvider
    {
        public DefaultUserIdProvider() { }
        public virtual string GetUserId(Microsoft.AspNetCore.SignalR.HubConnectionContext connection) { throw null; }
    }
    public abstract partial class DynamicHub : Microsoft.AspNetCore.SignalR.Hub
    {
        protected DynamicHub() { }
        public new Microsoft.AspNetCore.SignalR.DynamicHubClients Clients { get { throw null; } set { } }
    }
    public partial class DynamicHubClients
    {
        public DynamicHubClients(Microsoft.AspNetCore.SignalR.IHubCallerClients clients) { }
        public dynamic All { get { throw null; } }
        public dynamic Caller { get { throw null; } }
        public dynamic Others { get { throw null; } }
        public dynamic AllExcept(System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) { throw null; }
        public dynamic Client(string connectionId) { throw null; }
        public dynamic Clients(System.Collections.Generic.IReadOnlyList<string> connectionIds) { throw null; }
        public dynamic Group(string groupName) { throw null; }
        public dynamic GroupExcept(string groupName, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) { throw null; }
        public dynamic Groups(System.Collections.Generic.IReadOnlyList<string> groupNames) { throw null; }
        public dynamic OthersInGroup(string groupName) { throw null; }
        public dynamic User(string userId) { throw null; }
        public dynamic Users(System.Collections.Generic.IReadOnlyList<string> userIds) { throw null; }
    }
    public abstract partial class Hub : System.IDisposable
    {
        protected Hub() { }
        public Microsoft.AspNetCore.SignalR.IHubCallerClients Clients { get { throw null; } set { } }
        public Microsoft.AspNetCore.SignalR.HubCallerContext Context { get { throw null; } set { } }
        public Microsoft.AspNetCore.SignalR.IGroupManager Groups { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Threading.Tasks.Task OnConnectedAsync() { throw null; }
        public virtual System.Threading.Tasks.Task OnDisconnectedAsync(System.Exception exception) { throw null; }
    }
    public abstract partial class HubCallerContext
    {
        protected HubCallerContext() { }
        public abstract System.Threading.CancellationToken ConnectionAborted { get; }
        public abstract string ConnectionId { get; }
        public abstract Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get; }
        public abstract System.Collections.Generic.IDictionary<object, object> Items { get; }
        public abstract System.Security.Claims.ClaimsPrincipal User { get; }
        public abstract string UserIdentifier { get; }
        public abstract void Abort();
    }
    public static partial class HubClientsExtensions
    {
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, System.Collections.Generic.IEnumerable<string> excludedConnectionIds) { throw null; }
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string excludedConnectionId1) { throw null; }
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2) { throw null; }
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3) { throw null; }
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4) { throw null; }
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5) { throw null; }
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6) { throw null; }
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7) { throw null; }
        public static T AllExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7, string excludedConnectionId8) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, System.Collections.Generic.IEnumerable<string> connectionIds) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string connection1) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string connection1, string connection2) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string connection1, string connection2, string connection3) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6, string connection7) { throw null; }
        public static T Clients<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6, string connection7, string connection8) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, System.Collections.Generic.IEnumerable<string> excludedConnectionIds) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, string excludedConnectionId1) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7) { throw null; }
        public static T GroupExcept<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7, string excludedConnectionId8) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, System.Collections.Generic.IEnumerable<string> groupNames) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string group1) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string group1, string group2) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string group1, string group2, string group3) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string group1, string group2, string group3, string group4) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6, string group7) { throw null; }
        public static T Groups<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6, string group7, string group8) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, System.Collections.Generic.IEnumerable<string> userIds) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string user1) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string user1, string user2) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string user1, string user2, string user3) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string user1, string user2, string user3, string user4) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6, string user7) { throw null; }
        public static T Users<T>(this Microsoft.AspNetCore.SignalR.IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6, string user7, string user8) { throw null; }
    }
    public partial class HubConnectionContext
    {
        public HubConnectionContext(Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, Microsoft.AspNetCore.SignalR.HubConnectionContextOptions contextOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public virtual System.Threading.CancellationToken ConnectionAborted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual string ConnectionId { get { throw null; } }
        public virtual Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get { throw null; } }
        public virtual System.Collections.Generic.IDictionary<object, object> Items { get { throw null; } }
        public virtual Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol Protocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.Security.Claims.ClaimsPrincipal User { get { throw null; } }
        public string UserIdentifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void Abort() { }
        public virtual System.Threading.Tasks.ValueTask WriteAsync(Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.ValueTask WriteAsync(Microsoft.AspNetCore.SignalR.SerializedHubMessage message, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class HubConnectionContextOptions
    {
        public HubConnectionContextOptions() { }
        public System.TimeSpan ClientTimeoutInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan KeepAliveInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long? MaximumReceiveMessageSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int StreamBufferCapacity { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class HubConnectionHandler<THub> : Microsoft.AspNetCore.Connections.ConnectionHandler where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        public HubConnectionHandler(Microsoft.AspNetCore.SignalR.HubLifetimeManager<THub> lifetimeManager, Microsoft.AspNetCore.SignalR.IHubProtocolResolver protocolResolver, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.SignalR.HubOptions> globalHubOptions, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.SignalR.HubOptions<THub>> hubOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.SignalR.IUserIdProvider userIdProvider, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory serviceScopeFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task OnConnectedAsync(Microsoft.AspNetCore.Connections.ConnectionContext connection) { throw null; }
    }
    public partial class HubConnectionStore
    {
        public HubConnectionStore() { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.SignalR.HubConnectionContext this[string connectionId] { get { throw null; } }
        public void Add(Microsoft.AspNetCore.SignalR.HubConnectionContext connection) { }
        public Microsoft.AspNetCore.SignalR.HubConnectionStore.Enumerator GetEnumerator() { throw null; }
        public void Remove(Microsoft.AspNetCore.SignalR.HubConnectionContext connection) { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public readonly partial struct Enumerator : System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.SignalR.HubConnectionContext>, System.Collections.IEnumerator, System.IDisposable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public Enumerator(Microsoft.AspNetCore.SignalR.HubConnectionStore hubConnectionList) { throw null; }
            public Microsoft.AspNetCore.SignalR.HubConnectionContext Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class HubInvocationContext
    {
        public HubInvocationContext(Microsoft.AspNetCore.SignalR.HubCallerContext context, string hubMethodName, object[] hubMethodArguments) { }
        public HubInvocationContext(Microsoft.AspNetCore.SignalR.HubCallerContext context, System.Type hubType, string hubMethodName, object[] hubMethodArguments) { }
        public Microsoft.AspNetCore.SignalR.HubCallerContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<object> HubMethodArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string HubMethodName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type HubType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class HubLifetimeManager<THub> where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        protected HubLifetimeManager() { }
        public abstract System.Threading.Tasks.Task AddToGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task OnConnectedAsync(Microsoft.AspNetCore.SignalR.HubConnectionContext connection);
        public abstract System.Threading.Tasks.Task OnDisconnectedAsync(Microsoft.AspNetCore.SignalR.HubConnectionContext connection);
        public abstract System.Threading.Tasks.Task RemoveFromGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendAllAsync(string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendAllExceptAsync(string methodName, object[] args, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendConnectionAsync(string connectionId, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendConnectionsAsync(System.Collections.Generic.IReadOnlyList<string> connectionIds, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendGroupAsync(string groupName, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendGroupExceptAsync(string groupName, string methodName, object[] args, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendGroupsAsync(System.Collections.Generic.IReadOnlyList<string> groupNames, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendUserAsync(string userId, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task SendUsersAsync(System.Collections.Generic.IReadOnlyList<string> userIds, string methodName, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public partial class HubMetadata
    {
        public HubMetadata(System.Type hubType) { }
        public System.Type HubType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=true)]
    public partial class HubMethodNameAttribute : System.Attribute
    {
        public HubMethodNameAttribute(string name) { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class HubOptions
    {
        public HubOptions() { }
        public System.TimeSpan? ClientTimeoutInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool? EnableDetailedErrors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan? HandshakeTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan? KeepAliveInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long? MaximumReceiveMessageSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? StreamBufferCapacity { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<string> SupportedProtocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class HubOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.SignalR.HubOptions>
    {
        public HubOptionsSetup(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol> protocols) { }
        public void Configure(Microsoft.AspNetCore.SignalR.HubOptions options) { }
    }
    public partial class HubOptionsSetup<THub> : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.SignalR.HubOptions<THub>> where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        public HubOptionsSetup(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.SignalR.HubOptions> options) { }
        public void Configure(Microsoft.AspNetCore.SignalR.HubOptions<THub> options) { }
    }
    public partial class HubOptions<THub> : Microsoft.AspNetCore.SignalR.HubOptions where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        public HubOptions() { }
    }
    public abstract partial class Hub<T> : Microsoft.AspNetCore.SignalR.Hub where T : class
    {
        protected Hub() { }
        public new Microsoft.AspNetCore.SignalR.IHubCallerClients<T> Clients { get { throw null; } set { } }
    }
    public partial interface IClientProxy
    {
        System.Threading.Tasks.Task SendCoreAsync(string method, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public partial interface IGroupManager
    {
        System.Threading.Tasks.Task AddToGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.Threading.Tasks.Task RemoveFromGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public partial interface IHubActivator<THub> where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        THub Create();
        void Release(THub hub);
    }
    public partial interface IHubCallerClients : Microsoft.AspNetCore.SignalR.IHubCallerClients<Microsoft.AspNetCore.SignalR.IClientProxy>, Microsoft.AspNetCore.SignalR.IHubClients<Microsoft.AspNetCore.SignalR.IClientProxy>
    {
    }
    public partial interface IHubCallerClients<T> : Microsoft.AspNetCore.SignalR.IHubClients<T>
    {
        T Caller { get; }
        T Others { get; }
        T OthersInGroup(string groupName);
    }
    public partial interface IHubClients : Microsoft.AspNetCore.SignalR.IHubClients<Microsoft.AspNetCore.SignalR.IClientProxy>
    {
    }
    public partial interface IHubClients<T>
    {
        T All { get; }
        T AllExcept(System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds);
        T Client(string connectionId);
        T Clients(System.Collections.Generic.IReadOnlyList<string> connectionIds);
        T Group(string groupName);
        T GroupExcept(string groupName, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds);
        T Groups(System.Collections.Generic.IReadOnlyList<string> groupNames);
        T User(string userId);
        T Users(System.Collections.Generic.IReadOnlyList<string> userIds);
    }
    public partial interface IHubContext<THub> where THub : Microsoft.AspNetCore.SignalR.Hub
    {
        Microsoft.AspNetCore.SignalR.IHubClients Clients { get; }
        Microsoft.AspNetCore.SignalR.IGroupManager Groups { get; }
    }
    public partial interface IHubContext<THub, T> where THub : Microsoft.AspNetCore.SignalR.Hub<T> where T : class
    {
        Microsoft.AspNetCore.SignalR.IHubClients<T> Clients { get; }
        Microsoft.AspNetCore.SignalR.IGroupManager Groups { get; }
    }
    public partial interface IHubProtocolResolver
    {
        System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol> AllProtocols { get; }
        Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol GetProtocol(string protocolName, System.Collections.Generic.IReadOnlyList<string> supportedProtocols);
    }
    public partial interface ISignalRServerBuilder : Microsoft.AspNetCore.SignalR.ISignalRBuilder
    {
    }
    public partial interface IUserIdProvider
    {
        string GetUserId(Microsoft.AspNetCore.SignalR.HubConnectionContext connection);
    }
    public partial class SerializedHubMessage
    {
        public SerializedHubMessage(Microsoft.AspNetCore.SignalR.Protocol.HubMessage message) { }
        public SerializedHubMessage(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.SignalR.SerializedMessage> messages) { }
        public Microsoft.AspNetCore.SignalR.Protocol.HubMessage Message { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.ReadOnlyMemory<byte> GetSerializedMessage(Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol protocol) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct SerializedMessage
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public SerializedMessage(string protocolName, System.ReadOnlyMemory<byte> serialized) { throw null; }
        public string ProtocolName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.ReadOnlyMemory<byte> Serialized { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public static partial class SignalRConnectionBuilderExtensions
    {
        public static Microsoft.AspNetCore.Connections.IConnectionBuilder UseHub<THub>(this Microsoft.AspNetCore.Connections.IConnectionBuilder connectionBuilder) where THub : Microsoft.AspNetCore.SignalR.Hub { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SignalRDependencyInjectionExtensions
    {
        public static Microsoft.AspNetCore.SignalR.ISignalRServerBuilder AddSignalRCore(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
}
