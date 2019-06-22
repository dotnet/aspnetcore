# System.Net.Http.Headers

``` diff
-namespace System.Net.Http.Headers {
 {
-    public class CookieHeaderValue : ICloneable {
 {
-        protected CookieHeaderValue();

-        public CookieHeaderValue(string name, NameValueCollection values);

-        public CookieHeaderValue(string name, string value);

-        public Collection<CookieState> Cookies { get; }

-        public string Domain { get; set; }

-        public Nullable<DateTimeOffset> Expires { get; set; }

-        public bool HttpOnly { get; set; }

-        public Nullable<TimeSpan> MaxAge { get; set; }

-        public string Path { get; set; }

-        public bool Secure { get; set; }

-        public CookieState this[string name] { get; }

-        public object Clone();

-        public override string ToString();

-        public static bool TryParse(string input, out CookieHeaderValue parsedValue);

-    }
-    public class CookieState : ICloneable {
 {
-        public CookieState(string name);

-        public CookieState(string name, NameValueCollection values);

-        public CookieState(string name, string value);

-        public string Name { get; set; }

-        public string this[string subName] { get; set; }

-        public string Value { get; set; }

-        public NameValueCollection Values { get; }

-        public object Clone();

-        public override string ToString();

-    }
-}
```

