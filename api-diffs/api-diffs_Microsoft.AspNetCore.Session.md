# Microsoft.AspNetCore.Session

``` diff
 namespace Microsoft.AspNetCore.Session {
     public class DistributedSession : ISession {
         public DistributedSession(IDistributedCache cache, string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<bool> tryEstablishSession, ILoggerFactory loggerFactory, bool isNewSessionKey);
         public string Id { get; }
         public bool IsAvailable { get; }
         public IEnumerable<string> Keys { get; }
         public void Clear();
         public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken));
         public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken));
         public void Remove(string key);
         public void Set(string key, byte[] value);
         public bool TryGetValue(string key, out byte[] value);
     }
     public class DistributedSessionStore : ISessionStore {
         public DistributedSessionStore(IDistributedCache cache, ILoggerFactory loggerFactory);
         public ISession Create(string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<bool> tryEstablishSession, bool isNewSessionKey);
     }
     public interface ISessionStore {
         ISession Create(string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<bool> tryEstablishSession, bool isNewSessionKey);
     }
     public static class SessionDefaults {
         public static readonly string CookieName;
         public static readonly string CookiePath;
     }
     public class SessionFeature : ISessionFeature {
         public SessionFeature();
         public ISession Session { get; set; }
     }
     public class SessionMiddleware {
         public SessionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IDataProtectionProvider dataProtectionProvider, ISessionStore sessionStore, IOptions<SessionOptions> options);
         public Task Invoke(HttpContext context);
     }
 }
```

