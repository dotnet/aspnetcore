# Microsoft.Extensions.Configuration.CommandLine

``` diff
 namespace Microsoft.Extensions.Configuration.CommandLine {
     public class CommandLineConfigurationProvider : ConfigurationProvider {
         public CommandLineConfigurationProvider(IEnumerable<string> args, IDictionary<string, string> switchMappings = null);
         protected IEnumerable<string> Args { get; private set; }
         public override void Load();
     }
     public class CommandLineConfigurationSource : IConfigurationSource {
         public CommandLineConfigurationSource();
         public IEnumerable<string> Args { get; set; }
         public IDictionary<string, string> SwitchMappings { get; set; }
         public IConfigurationProvider Build(IConfigurationBuilder builder);
     }
 }
```

