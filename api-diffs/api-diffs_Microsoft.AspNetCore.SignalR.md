# Microsoft.AspNetCore.SignalR

``` diff
 namespace Microsoft.AspNetCore.SignalR {
     public static class ClientProxyExtensions {
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendAsync(this IClientProxy clientProxy, string method, CancellationToken cancellationToken = default(CancellationToken));
     }
     public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub {
         public DefaultHubLifetimeManager(ILogger<DefaultHubLifetimeManager<THub>> logger);
         public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default(CancellationToken));
         public override Task OnConnectedAsync(HubConnectionContext connection);
         public override Task OnDisconnectedAsync(HubConnectionContext connection);
         public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
     }
     public class DefaultUserIdProvider : IUserIdProvider {
         public DefaultUserIdProvider();
         public virtual string GetUserId(HubConnectionContext connection);
     }
     public abstract class DynamicHub : Hub {
         protected DynamicHub();
         public new DynamicHubClients Clients { get; set; }
     }
     public class DynamicHubClients {
         public DynamicHubClients(IHubCallerClients clients);
         public dynamic All { get; }
         public dynamic Caller { get; }
         public dynamic Others { get; }
         public dynamic AllExcept(IReadOnlyList<string> excludedConnectionIds);
         public dynamic Client(string connectionId);
         public dynamic Clients(IReadOnlyList<string> connectionIds);
         public dynamic Group(string groupName);
         public dynamic GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds);
         public dynamic Groups(IReadOnlyList<string> groupNames);
         public dynamic OthersInGroup(string groupName);
         public dynamic User(string userId);
         public dynamic Users(IReadOnlyList<string> userIds);
     }
     public static class GetHttpContextExtensions {
         public static HttpContext GetHttpContext(this HubCallerContext connection);
         public static HttpContext GetHttpContext(this HubConnectionContext connection);
     }
     public abstract class Hub : IDisposable {
         protected Hub();
         public IHubCallerClients Clients { get; set; }
         public HubCallerContext Context { get; set; }
         public IGroupManager Groups { get; set; }
         public void Dispose();
         protected virtual void Dispose(bool disposing);
         public virtual Task OnConnectedAsync();
         public virtual Task OnDisconnectedAsync(Exception exception);
     }
     public abstract class Hub<T> : Hub where T : class {
         protected Hub();
         public new IHubCallerClients<T> Clients { get; set; }
     }
     public abstract class HubCallerContext {
         protected HubCallerContext();
         public abstract CancellationToken ConnectionAborted { get; }
         public abstract string ConnectionId { get; }
         public abstract IFeatureCollection Features { get; }
         public abstract IDictionary<object, object> Items { get; }
         public abstract ClaimsPrincipal User { get; }
         public abstract string UserIdentifier { get; }
         public abstract void Abort();
     }
     public static class HubClientsExtensions {
         public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1);
         public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2);
         public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3);
         public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4);
         public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5);
         public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6);
         public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7);
         public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7, string excludedConnectionId8);
         public static T Clients<T>(this IHubClients<T> hubClients, string connection1);
         public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2);
         public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3);
         public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4);
         public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5);
         public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6);
         public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6, string connection7);
         public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6, string connection7, string connection8);
         public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1);
         public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2);
         public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3);
         public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4);
         public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5);
         public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6);
         public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7);
         public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7, string excludedConnectionId8);
         public static T Groups<T>(this IHubClients<T> hubClients, string group1);
         public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2);
         public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3);
         public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4);
         public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5);
         public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6);
         public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6, string group7);
         public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6, string group7, string group8);
         public static T Users<T>(this IHubClients<T> hubClients, string user1);
         public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2);
         public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3);
         public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4);
         public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5);
         public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6);
         public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6, string user7);
         public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6, string user7, string user8);
     }
     public class HubConnectionContext {
         public HubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory);
         public HubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory, TimeSpan clientTimeoutInterval);
+        public HubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory, TimeSpan clientTimeoutInterval, int streamBufferCapacity);
         public virtual CancellationToken ConnectionAborted { get; }
         public virtual string ConnectionId { get; }
         public virtual IFeatureCollection Features { get; }
         public virtual IDictionary<object, object> Items { get; }
         public virtual IHubProtocol Protocol { get; set; }
         public virtual ClaimsPrincipal User { get; }
         public string UserIdentifier { get; set; }
         public virtual void Abort();
         public virtual ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken = default(CancellationToken));
         public virtual ValueTask WriteAsync(SerializedHubMessage message, CancellationToken cancellationToken = default(CancellationToken));
     }
     public class HubConnectionHandler<THub> : ConnectionHandler where THub : Hub {
         public HubConnectionHandler(HubLifetimeManager<THub> lifetimeManager, IHubProtocolResolver protocolResolver, IOptions<HubOptions> globalHubOptions, IOptions<HubOptions<THub>> hubOptions, ILoggerFactory loggerFactory, IUserIdProvider userIdProvider, HubDispatcher<THub> dispatcher);
         public override Task OnConnectedAsync(ConnectionContext connection);
     }
     public class HubConnectionStore {
         public HubConnectionStore();
         public int Count { get; }
         public HubConnectionContext this[string connectionId] { get; }
         public void Add(HubConnectionContext connection);
         public HubConnectionStore.Enumerator GetEnumerator();
         public void Remove(HubConnectionContext connection);
         public readonly struct Enumerator : IDisposable, IEnumerator, IEnumerator<HubConnectionContext> {
             public Enumerator(HubConnectionStore hubConnectionList);
             public HubConnectionContext Current { get; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             public void Reset();
         }
     }
+    public sealed class HubEndpointConventionBuilder : IEndpointConventionBuilder, IHubEndpointConventionBuilder {
+        public void Add(Action<EndpointBuilder> convention);
+    }
     public class HubException : Exception {
         public HubException();
         public HubException(SerializationInfo info, StreamingContext context);
         public HubException(string message);
         public HubException(string message, Exception innerException);
     }
     public abstract class HubLifetimeManager<THub> where THub : Hub {
         protected HubLifetimeManager();
         public abstract Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task OnConnectedAsync(HubConnectionContext connection);
         public abstract Task OnDisconnectedAsync(HubConnectionContext connection);
         public abstract Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
     }
+    public class HubMetadata {
+        public HubMetadata(Type hubType);
+        public Type HubType { get; }
+    }
     public class HubMethodNameAttribute : Attribute {
         public HubMethodNameAttribute(string name);
         public string Name { get; }
     }
     public class HubOptions {
         public HubOptions();
         public Nullable<TimeSpan> ClientTimeoutInterval { get; set; }
         public Nullable<bool> EnableDetailedErrors { get; set; }
         public Nullable<TimeSpan> HandshakeTimeout { get; set; }
         public Nullable<TimeSpan> KeepAliveInterval { get; set; }
+        public Nullable<long> MaximumReceiveMessageSize { get; set; }
+        public Nullable<int> StreamBufferCapacity { get; set; }
         public IList<string> SupportedProtocols { get; set; }
     }
     public class HubOptions<THub> : HubOptions where THub : Hub {
         public HubOptions();
     }
     public class HubRouteBuilder {
         public HubRouteBuilder(ConnectionsRouteBuilder routes);
         public void MapHub<THub>(PathString path) where THub : Hub;
         public void MapHub<THub>(PathString path, Action<HttpConnectionDispatcherOptions> configureOptions) where THub : Hub;
     }
     public interface IClientProxy {
         Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default(CancellationToken));
     }
     public interface IGroupManager {
         Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default(CancellationToken));
         Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default(CancellationToken));
     }
     public interface IHubActivator<THub> where THub : Hub {
         THub Create();
         void Release(THub hub);
     }
     public interface IHubCallerClients : IHubCallerClients<IClientProxy>, IHubClients<IClientProxy>
     public interface IHubCallerClients<T> : IHubClients<T> {
         T Caller { get; }
         T Others { get; }
         T OthersInGroup(string groupName);
     }
     public interface IHubClients : IHubClients<IClientProxy>
     public interface IHubClients<T> {
         T All { get; }
         T AllExcept(IReadOnlyList<string> excludedConnectionIds);
         T Client(string connectionId);
         T Clients(IReadOnlyList<string> connectionIds);
         T Group(string groupName);
         T GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds);
         T Groups(IReadOnlyList<string> groupNames);
         T User(string userId);
         T Users(IReadOnlyList<string> userIds);
     }
     public interface IHubContext<THub> where THub : Hub {
         IHubClients Clients { get; }
         IGroupManager Groups { get; }
     }
     public interface IHubContext<THub, T> where THub : Hub<T> where T : class {
         IHubClients<T> Clients { get; }
         IGroupManager Groups { get; }
     }
+    public interface IHubEndpointConventionBuilder : IEndpointConventionBuilder
     public interface IHubProtocolResolver {
         IReadOnlyList<IHubProtocol> AllProtocols { get; }
         IHubProtocol GetProtocol(string protocolName, IReadOnlyList<string> supportedProtocols);
     }
     public interface IInvocationBinder {
         IReadOnlyList<Type> GetParameterTypes(string methodName);
         Type GetReturnType(string invocationId);
+        Type GetStreamItemType(string streamId);
     }
     public interface ISignalRBuilder {
         IServiceCollection Services { get; }
     }
     public interface ISignalRServerBuilder : ISignalRBuilder
     public interface IUserIdProvider {
         string GetUserId(HubConnectionContext connection);
     }
     public class JsonHubProtocolOptions {
         public JsonHubProtocolOptions();
+        public JsonSerializerOptions PayloadSerializerOptions { get; set; }
-        public JsonSerializerSettings PayloadSerializerSettings { get; set; }

     }
     public class SerializedHubMessage {
         public SerializedHubMessage(HubMessage message);
         public SerializedHubMessage(IReadOnlyList<SerializedMessage> messages);
         public HubMessage Message { get; }
         public ReadOnlyMemory<byte> GetSerializedMessage(IHubProtocol protocol);
     }
     public readonly struct SerializedMessage {
         public SerializedMessage(string protocolName, ReadOnlyMemory<byte> serialized);
         public string ProtocolName { get; }
         public ReadOnlyMemory<byte> Serialized { get; }
     }
     public static class SignalRConnectionBuilderExtensions {
         public static IConnectionBuilder UseHub<THub>(this IConnectionBuilder connectionBuilder) where THub : Hub;
     }
 }
```

