# Microsoft.AspNetCore.Server.HttpSys

``` diff
 namespace Microsoft.AspNetCore.Server.HttpSys {
     public sealed class AuthenticationManager {
         public bool AllowAnonymous { get; set; }
         public AuthenticationSchemes Schemes { get; set; }
     }
     public enum AuthenticationSchemes {
         Basic = 1,
         Kerberos = 16,
         Negotiate = 8,
         None = 0,
         NTLM = 4,
     }
     public enum Http503VerbosityLevel : long {
         Basic = (long)0,
         Full = (long)2,
         Limited = (long)1,
     }
     public static class HttpSysDefaults {
-        public static readonly string AuthenticationScheme;
+        public const string AuthenticationScheme = "Windows";
     }
     public class HttpSysException : Win32Exception {
         public override int ErrorCode { get; }
     }
     public class HttpSysOptions {
         public HttpSysOptions();
         public bool AllowSynchronousIO { get; set; }
         public AuthenticationManager Authentication { get; }
         public bool EnableResponseCaching { get; set; }
         public Http503VerbosityLevel Http503Verbosity { get; set; }
         public int MaxAccepts { get; set; }
         public Nullable<long> MaxConnections { get; set; }
         public Nullable<long> MaxRequestBodySize { get; set; }
         public long RequestQueueLimit { get; set; }
         public bool ThrowWriteExceptions { get; set; }
         public TimeoutManager Timeouts { get; }
         public UrlPrefixCollection UrlPrefixes { get; }
     }
     public sealed class TimeoutManager {
         public TimeSpan DrainEntityBody { get; set; }
         public TimeSpan EntityBody { get; set; }
         public TimeSpan HeaderWait { get; set; }
         public TimeSpan IdleConnection { get; set; }
         public long MinSendBytesPerSecond { get; set; }
         public TimeSpan RequestQueue { get; set; }
     }
     public class UrlPrefix {
         public string FullPrefix { get; private set; }
         public string Host { get; private set; }
         public bool IsHttps { get; private set; }
         public string Path { get; private set; }
         public string Port { get; private set; }
         public int PortValue { get; private set; }
         public string Scheme { get; private set; }
         public static UrlPrefix Create(string prefix);
         public static UrlPrefix Create(string scheme, string host, Nullable<int> portValue, string path);
         public static UrlPrefix Create(string scheme, string host, string port, string path);
         public override bool Equals(object obj);
         public override int GetHashCode();
         public override string ToString();
     }
     public class UrlPrefixCollection : ICollection<UrlPrefix>, IEnumerable, IEnumerable<UrlPrefix> {
         public int Count { get; }
         public bool IsReadOnly { get; }
         public void Add(UrlPrefix item);
         public void Add(string prefix);
         public void Clear();
         public bool Contains(UrlPrefix item);
         public void CopyTo(UrlPrefix[] array, int arrayIndex);
         public IEnumerator<UrlPrefix> GetEnumerator();
         public bool Remove(UrlPrefix item);
         public bool Remove(string prefix);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
 }
```

