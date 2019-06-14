# Microsoft.DotNet.PlatformAbstractions

``` diff
-namespace Microsoft.DotNet.PlatformAbstractions {
 {
-    public static class ApplicationEnvironment {
 {
-        public static string ApplicationBasePath { get; }

-    }
-    public struct HashCodeCombiner {
 {
-        public int CombinedHash { get; }

-        public void Add(int i);

-        public void Add(object o);

-        public void Add(string s);

-        public void Add<TValue>(TValue value, IEqualityComparer<TValue> comparer);

-        public static HashCodeCombiner Start();

-    }
-    public enum Platform {
 {
-        Darwin = 3,

-        FreeBSD = 4,

-        Linux = 2,

-        Unknown = 0,

-        Windows = 1,

-    }
-    public static class RuntimeEnvironment {
 {
-        public static string OperatingSystem { get; }

-        public static Platform OperatingSystemPlatform { get; }

-        public static string OperatingSystemVersion { get; }

-        public static string RuntimeArchitecture { get; }

-        public static string GetRuntimeIdentifier();

-    }
-}
```

