# Microsoft.AspNetCore.SignalR

``` diff
 namespace Microsoft.AspNetCore.SignalR {
     public class HubConnectionContext {
+        public HubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory, TimeSpan clientTimeoutInterval, int streamBufferCapacity);
     }
+    public sealed class HubEndpointConventionBuilder : IEndpointConventionBuilder, IHubEndpointConventionBuilder {
+        public void Add(Action<EndpointBuilder> convention);
+    }
+    public class HubMetadata {
+        public HubMetadata(Type hubType);
+        public Type HubType { get; }
+    }
     public class HubOptions {
+        public Nullable<long> MaximumReceiveMessageSize { get; set; }
+        public Nullable<int> StreamBufferCapacity { get; set; }
     }
+    public interface IHubEndpointConventionBuilder : IEndpointConventionBuilder
     public interface IInvocationBinder {
+        Type GetStreamItemType(string streamId);
     }
     public class JsonHubProtocolOptions {
+        public JsonSerializerOptions PayloadSerializerOptions { get; set; }
-        public JsonSerializerSettings PayloadSerializerSettings { get; set; }

     }
 }
```

