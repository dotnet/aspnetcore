# Microsoft.AspNetCore.Connections.Features

``` diff
 namespace Microsoft.AspNetCore.Connections.Features {
+    public interface IConnectionCompleteFeature {
+        void OnCompleted(Func<object, Task> callback, object state);
+    }
+    public interface IConnectionEndPointFeature {
+        EndPoint LocalEndPoint { get; set; }
+        EndPoint RemoteEndPoint { get; set; }
+    }
     public interface IConnectionHeartbeatFeature {
         void OnHeartbeat(Action<object> action, object state);
     }
     public interface IConnectionIdFeature {
         string ConnectionId { get; set; }
     }
     public interface IConnectionInherentKeepAliveFeature {
         bool HasInherentKeepAlive { get; }
     }
     public interface IConnectionItemsFeature {
         IDictionary<object, object> Items { get; set; }
     }
     public interface IConnectionLifetimeFeature {
         CancellationToken ConnectionClosed { get; set; }
         void Abort();
     }
     public interface IConnectionLifetimeNotificationFeature {
         CancellationToken ConnectionClosedRequested { get; set; }
         void RequestClose();
     }
     public interface IConnectionTransportFeature {
         IDuplexPipe Transport { get; set; }
     }
     public interface IConnectionUserFeature {
         ClaimsPrincipal User { get; set; }
     }
     public interface IMemoryPoolFeature {
         MemoryPool<byte> MemoryPool { get; }
     }
     public interface ITlsHandshakeFeature {
         CipherAlgorithmType CipherAlgorithm { get; }
         int CipherStrength { get; }
         HashAlgorithmType HashAlgorithm { get; }
         int HashStrength { get; }
         ExchangeAlgorithmType KeyExchangeAlgorithm { get; }
         int KeyExchangeStrength { get; }
         SslProtocols Protocol { get; }
     }
     public interface ITransferFormatFeature {
         TransferFormat ActiveFormat { get; set; }
         TransferFormat SupportedFormats { get; }
     }
 }
```

