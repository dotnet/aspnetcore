# Microsoft.AspNetCore.SignalR.Protocol

``` diff
 namespace Microsoft.AspNetCore.SignalR.Protocol {
     public static class HandshakeProtocol {
-        public static ReadOnlyMemory<byte> SuccessHandshakeData;

+        public static ReadOnlySpan<byte> GetSuccessfulHandshake(IHubProtocol protocol);
     }
     public abstract class HubMethodInvocationMessage : HubInvocationMessage {
+        protected HubMethodInvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds);
+        public string[] StreamIds { get; }
     }
     public class InvocationMessage : HubMethodInvocationMessage {
+        public InvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds);
     }
-    public class JsonHubProtocol : IHubProtocol {
+    public sealed class JsonHubProtocol : IHubProtocol {
-        public JsonSerializer PayloadSerializer { get; }

     }
+    public class StreamBindingFailureMessage : HubMessage {
+        public StreamBindingFailureMessage(string id, ExceptionDispatchInfo bindingFailure);
+        public ExceptionDispatchInfo BindingFailure { get; }
+        public string Id { get; }
+    }
     public class StreamInvocationMessage : HubMethodInvocationMessage {
+        public StreamInvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds);
     }
 }
```

