# Microsoft.AspNetCore.StaticFiles.Infrastructure

``` diff
 namespace Microsoft.AspNetCore.StaticFiles.Infrastructure {
     public class SharedOptions {
         public SharedOptions();
         public IFileProvider FileProvider { get; set; }
         public PathString RequestPath { get; set; }
     }
     public abstract class SharedOptionsBase {
         protected SharedOptionsBase(SharedOptions sharedOptions);
         public IFileProvider FileProvider { get; set; }
         public PathString RequestPath { get; set; }
         protected SharedOptions SharedOptions { get; private set; }
     }
 }
```

