# Microsoft.Extensions.Logging.Internal

``` diff
-namespace Microsoft.Extensions.Logging.Internal {
 {
-    public class FormattedLogValues : IEnumerable, IEnumerable<KeyValuePair<string, object>>, IReadOnlyCollection<KeyValuePair<string, object>>, IReadOnlyList<KeyValuePair<string, object>> {
 {
-        public FormattedLogValues(string format, params object[] values);

-        public int Count { get; }

-        public KeyValuePair<string, object> this[int index] { get; }

-        public IEnumerator<KeyValuePair<string, object>> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public override string ToString();

-    }
-    public class LogValuesFormatter {
 {
-        public LogValuesFormatter(string format);

-        public string OriginalFormat { get; private set; }

-        public List<string> ValueNames { get; }

-        public string Format(object[] values);

-        public KeyValuePair<string, object> GetValue(object[] values, int index);

-        public IEnumerable<KeyValuePair<string, object>> GetValues(object[] values);

-    }
-}
```

