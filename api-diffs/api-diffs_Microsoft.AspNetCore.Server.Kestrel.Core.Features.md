# Microsoft.AspNetCore.Server.Kestrel.Core.Features

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features {
     public interface IConnectionTimeoutFeature {
         void CancelTimeout();
         void ResetTimeout(TimeSpan timeSpan);
         void SetTimeout(TimeSpan timeSpan);
     }
     public interface IDecrementConcurrentConnectionCountFeature {
         void ReleaseConnection();
     }
     public interface IHttp2StreamIdFeature {
         int StreamId { get; }
     }
     public interface IHttpMinRequestBodyDataRateFeature {
         MinDataRate MinDataRate { get; set; }
     }
     public interface IHttpMinResponseDataRateFeature {
         MinDataRate MinDataRate { get; set; }
     }
     public interface ITlsApplicationProtocolFeature {
         ReadOnlyMemory<byte> ApplicationProtocol { get; }
     }
 }
```

