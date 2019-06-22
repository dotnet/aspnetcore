# Microsoft.AspNetCore.SignalR.Internal

``` diff
-namespace Microsoft.AspNetCore.SignalR.Internal {
 {
-    public class DefaultHubActivator<THub> : IHubActivator<THub> where THub : Hub {
 {
-        public DefaultHubActivator(IServiceProvider serviceProvider);

-        public virtual THub Create();

-        public virtual void Release(THub hub);

-    }
-    public class DefaultHubCallerContext : HubCallerContext {
 {
-        public DefaultHubCallerContext(HubConnectionContext connection);

-        public override CancellationToken ConnectionAborted { get; }

-        public override string ConnectionId { get; }

-        public override IFeatureCollection Features { get; }

-        public override IDictionary<object, object> Items { get; }

-        public override ClaimsPrincipal User { get; }

-        public override string UserIdentifier { get; }

-        public override void Abort();

-    }
-    public class DefaultHubDispatcher<THub> : HubDispatcher<THub> where THub : Hub {
 {
-        public DefaultHubDispatcher(IServiceScopeFactory serviceScopeFactory, IHubContext<THub> hubContext, IOptions<HubOptions<THub>> hubOptions, IOptions<HubOptions> globalHubOptions, ILogger<DefaultHubDispatcher<THub>> logger);

-        public override Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage);

-        public override IReadOnlyList<Type> GetParameterTypes(string methodName);

-        public override Type GetReturnType(string invocationId);

-        public override Task OnConnectedAsync(HubConnectionContext connection);

-        public override Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception);

-    }
-    public class DefaultHubProtocolResolver : IHubProtocolResolver {
 {
-        public DefaultHubProtocolResolver(IEnumerable<IHubProtocol> availableProtocols, ILogger<DefaultHubProtocolResolver> logger);

-        public IReadOnlyList<IHubProtocol> AllProtocols { get; }

-        public virtual IHubProtocol GetProtocol(string protocolName, IReadOnlyList<string> supportedProtocols);

-    }
-    public class HubCallerClients : IHubCallerClients, IHubCallerClients<IClientProxy>, IHubClients<IClientProxy> {
 {
-        public HubCallerClients(IHubClients hubClients, string connectionId);

-        public IClientProxy All { get; }

-        public IClientProxy Caller { get; }

-        public IClientProxy Others { get; }

-        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds);

-        public IClientProxy Client(string connectionId);

-        public IClientProxy Clients(IReadOnlyList<string> connectionIds);

-        public IClientProxy Group(string groupName);

-        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds);

-        public IClientProxy Groups(IReadOnlyList<string> groupNames);

-        public IClientProxy OthersInGroup(string groupName);

-        public IClientProxy User(string userId);

-        public IClientProxy Users(IReadOnlyList<string> userIds);

-    }
-    public abstract class HubDispatcher<THub> : IInvocationBinder where THub : Hub {
 {
-        protected HubDispatcher();

-        public abstract Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage);

-        public abstract IReadOnlyList<Type> GetParameterTypes(string methodName);

-        public abstract Type GetReturnType(string invocationId);

-        public abstract Task OnConnectedAsync(HubConnectionContext connection);

-        public abstract Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception);

-    }
-    public class HubOptionsSetup : IConfigureOptions<HubOptions> {
 {
-        public HubOptionsSetup(IEnumerable<IHubProtocol> protocols);

-        public void Configure(HubOptions options);

-    }
-    public class HubOptionsSetup<THub> : IConfigureOptions<HubOptions<THub>> where THub : Hub {
 {
-        public HubOptionsSetup(IOptions<HubOptions> options);

-        public void Configure(HubOptions<THub> options);

-    }
-    public static class HubReflectionHelper {
 {
-        public static IEnumerable<MethodInfo> GetHubMethods(Type hubType);

-    }
-    public static class TypeBaseEnumerationExtensions {
 {
-        public static IEnumerable<Type> AllBaseTypes(this Type type);

-    }
-}
```

