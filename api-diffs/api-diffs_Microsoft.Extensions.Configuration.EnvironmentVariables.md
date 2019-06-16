# Microsoft.Extensions.Configuration.EnvironmentVariables

``` diff
 namespace Microsoft.Extensions.Configuration.EnvironmentVariables {
     public class EnvironmentVariablesConfigurationProvider : ConfigurationProvider {
         public EnvironmentVariablesConfigurationProvider();
         public EnvironmentVariablesConfigurationProvider(string prefix);
         public override void Load();
     }
     public class EnvironmentVariablesConfigurationSource : IConfigurationSource {
         public EnvironmentVariablesConfigurationSource();
         public string Prefix { get; set; }
         public IConfigurationProvider Build(IConfigurationBuilder builder);
     }
 }
```

