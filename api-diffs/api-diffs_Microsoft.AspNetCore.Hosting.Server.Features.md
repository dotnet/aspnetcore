# Microsoft.AspNetCore.Hosting.Server.Features

``` diff
 namespace Microsoft.AspNetCore.Hosting.Server.Features {
     public interface IServerAddressesFeature {
         ICollection<string> Addresses { get; }
         bool PreferHostingUrls { get; set; }
     }
     public class ServerAddressesFeature : IServerAddressesFeature {
         public ServerAddressesFeature();
         public ICollection<string> Addresses { get; }
         public bool PreferHostingUrls { get; set; }
     }
 }
```

