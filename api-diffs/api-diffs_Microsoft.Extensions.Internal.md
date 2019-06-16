# Microsoft.Extensions.Internal

``` diff
 namespace Microsoft.Extensions.Internal {
     public interface ISystemClock {
         DateTimeOffset UtcNow { get; }
     }
     public class SystemClock : ISystemClock {
         public SystemClock();
         public DateTimeOffset UtcNow { get; }
     }
 }
```

