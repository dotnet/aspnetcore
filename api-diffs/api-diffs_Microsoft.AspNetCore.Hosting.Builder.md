# Microsoft.AspNetCore.Hosting.Builder

``` diff
 namespace Microsoft.AspNetCore.Hosting.Builder {
     public class ApplicationBuilderFactory : IApplicationBuilderFactory {
         public ApplicationBuilderFactory(IServiceProvider serviceProvider);
         public IApplicationBuilder CreateBuilder(IFeatureCollection serverFeatures);
     }
     public interface IApplicationBuilderFactory {
         IApplicationBuilder CreateBuilder(IFeatureCollection serverFeatures);
     }
 }
```

