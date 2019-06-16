# Microsoft.AspNetCore.DataProtection.Internal

``` diff
 namespace Microsoft.AspNetCore.DataProtection.Internal {
     public class DataProtectionBuilder : IDataProtectionBuilder {
         public DataProtectionBuilder(IServiceCollection services);
         public IServiceCollection Services { get; }
     }
     public interface IActivator {
         object CreateInstance(Type expectedBaseType, string implementationTypeName);
     }
 }
```

