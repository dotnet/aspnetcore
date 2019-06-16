# Microsoft.AspNetCore.SignalR.Protocol

``` diff
 namespace Microsoft.AspNetCore.SignalR.Protocol {
     public class CancelInvocationMessage : HubInvocationMessage {
         public CancelInvocationMessage(string invocationId);
     }
     public class CloseMessage : HubMessage {
         public static readonly CloseMessage Empty;
         public CloseMessage(string error);
         public string Error { get; }
     }
     public class CompletionMessage : HubInvocationMessage {
         public CompletionMessage(string invocationId, string error, object result, bool hasResult);
         public string Error { get; }
         public bool HasResult { get; }
         public object Result { get; }
         public static CompletionMessage Empty(string invocationId);
         public override string ToString();
         public static CompletionMessage WithError(string invocationId, string error);
         public static CompletionMessage WithResult(string invocationId, object payload);
     }
     public static class HandshakeProtocol {
-        public static ReadOnlyMemory<byte> SuccessHandshakeData;

+        public static ReadOnlySpan<byte> GetSuccessfulHandshake(IHubProtocol protocol);
         public static bool TryParseRequestMessage(ref ReadOnlySequence<byte> buffer, out HandshakeRequestMessage requestMessage);
         public static bool TryParseResponseMessage(ref ReadOnlySequence<byte> buffer, out HandshakeResponseMessage responseMessage);
         public static void WriteRequestMessage(HandshakeRequestMessage requestMessage, IBufferWriter<byte> output);
         public static void WriteResponseMessage(HandshakeResponseMessage responseMessage, IBufferWriter<byte> output);
     }
     public class HandshakeRequestMessage : HubMessage {
         public HandshakeRequestMessage(string protocol, int version);
         public string Protocol { get; }
         public int Version { get; }
     }
     public class HandshakeResponseMessage : HubMessage {
         public static readonly HandshakeResponseMessage Empty;
         public HandshakeResponseMessage(string error);
         public string Error { get; }
     }
     public abstract class HubInvocationMessage : HubMessage {
         protected HubInvocationMessage(string invocationId);
         public IDictionary<string, string> Headers { get; set; }
         public string InvocationId { get; }
     }
     public abstract class HubMessage {
         protected HubMessage();
     }
     public abstract class HubMethodInvocationMessage : HubInvocationMessage {
         protected HubMethodInvocationMessage(string invocationId, string target, object[] arguments);
+        protected HubMethodInvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds);
         public object[] Arguments { get; }
+        public string[] StreamIds { get; }
         public string Target { get; }
     }
     public static class HubProtocolConstants {
         public const int CancelInvocationMessageType = 5;
         public const int CloseMessageType = 7;
         public const int CompletionMessageType = 3;
         public const int InvocationMessageType = 1;
         public const int PingMessageType = 6;
         public const int StreamInvocationMessageType = 4;
         public const int StreamItemMessageType = 2;
     }
     public static class HubProtocolExtensions {
         public static byte[] GetMessageBytes(this IHubProtocol hubProtocol, HubMessage message);
     }
     public interface IHubProtocol {
         string Name { get; }
         TransferFormat TransferFormat { get; }
         int Version { get; }
         ReadOnlyMemory<byte> GetMessageBytes(HubMessage message);
         bool IsVersionSupported(int version);
         bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message);
         void WriteMessage(HubMessage message, IBufferWriter<byte> output);
     }
     public class InvocationBindingFailureMessage : HubInvocationMessage {
         public InvocationBindingFailureMessage(string invocationId, string target, ExceptionDispatchInfo bindingFailure);
         public ExceptionDispatchInfo BindingFailure { get; }
         public string Target { get; }
     }
     public class InvocationMessage : HubMethodInvocationMessage {
         public InvocationMessage(string target, object[] arguments);
         public InvocationMessage(string invocationId, string target, object[] arguments);
+        public InvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds);
         public override string ToString();
     }
-    public class JsonHubProtocol : IHubProtocol {
+    public sealed class JsonHubProtocol : IHubProtocol {
         public JsonHubProtocol();
         public JsonHubProtocol(IOptions<JsonHubProtocolOptions> options);
         public string Name { get; }
-        public JsonSerializer PayloadSerializer { get; }

         public TransferFormat TransferFormat { get; }
         public int Version { get; }
         public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message);
         public bool IsVersionSupported(int version);
         public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message);
         public void WriteMessage(HubMessage message, IBufferWriter<byte> output);
     }
     public class PingMessage : HubMessage {
         public static readonly PingMessage Instance;
     }
+    public class StreamBindingFailureMessage : HubMessage {
+        public StreamBindingFailureMessage(string id, ExceptionDispatchInfo bindingFailure);
+        public ExceptionDispatchInfo BindingFailure { get; }
+        public string Id { get; }
+    }
     public class StreamInvocationMessage : HubMethodInvocationMessage {
         public StreamInvocationMessage(string invocationId, string target, object[] arguments);
+        public StreamInvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds);
         public override string ToString();
     }
     public class StreamItemMessage : HubInvocationMessage {
         public StreamItemMessage(string invocationId, object item);
         public object Item { get; }
         public override string ToString();
     }
 }
```

