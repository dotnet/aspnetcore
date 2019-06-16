# Microsoft.AspNetCore.Http.Extensions

``` diff
 namespace Microsoft.AspNetCore.Http.Extensions {
     public static class HttpRequestMultipartExtensions {
         public static string GetMultipartBoundary(this HttpRequest request);
     }
     public class QueryBuilder : IEnumerable, IEnumerable<KeyValuePair<string, string>> {
         public QueryBuilder();
         public QueryBuilder(IEnumerable<KeyValuePair<string, string>> parameters);
         public void Add(string key, IEnumerable<string> values);
         public void Add(string key, string value);
         public override bool Equals(object obj);
         public IEnumerator<KeyValuePair<string, string>> GetEnumerator();
         public override int GetHashCode();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public QueryString ToQueryString();
         public override string ToString();
     }
     public static class StreamCopyOperation {
         public static Task CopyToAsync(Stream source, Stream destination, Nullable<long> count, int bufferSize, CancellationToken cancel);
         public static Task CopyToAsync(Stream source, Stream destination, Nullable<long> count, CancellationToken cancel);
     }
     public static class UriHelper {
         public static string BuildAbsolute(string scheme, HostString host, PathString pathBase = default(PathString), PathString path = default(PathString), QueryString query = default(QueryString), FragmentString fragment = default(FragmentString));
         public static string BuildRelative(PathString pathBase = default(PathString), PathString path = default(PathString), QueryString query = default(QueryString), FragmentString fragment = default(FragmentString));
         public static string Encode(Uri uri);
         public static void FromAbsolute(string uri, out string scheme, out HostString host, out PathString path, out QueryString query, out FragmentString fragment);
         public static string GetDisplayUrl(this HttpRequest request);
         public static string GetEncodedPathAndQuery(this HttpRequest request);
         public static string GetEncodedUrl(this HttpRequest request);
     }
 }
```

