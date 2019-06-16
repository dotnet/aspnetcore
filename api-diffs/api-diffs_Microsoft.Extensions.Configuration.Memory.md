# Microsoft.Extensions.Configuration.Memory

``` diff
 namespace Microsoft.Extensions.Configuration.Memory {
     public class MemoryConfigurationProvider : ConfigurationProvider, IEnumerable, IEnumerable<KeyValuePair<string, string>> {
         public MemoryConfigurationProvider(MemoryConfigurationSource source);
         public void Add(string key, string value);
         public IEnumerator<KeyValuePair<string, string>> GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public class MemoryConfigurationSource : IConfigurationSource {
         public MemoryConfigurationSource();
         public IEnumerable<KeyValuePair<string, string>> InitialData { get; set; }
         public IConfigurationProvider Build(IConfigurationBuilder builder);
     }
 }
```

