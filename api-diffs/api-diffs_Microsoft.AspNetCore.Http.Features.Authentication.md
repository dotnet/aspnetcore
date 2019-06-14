# Microsoft.AspNetCore.Http.Features.Authentication

``` diff
 namespace Microsoft.AspNetCore.Http.Features.Authentication {
-    public class AuthenticateContext {
 {
-        public AuthenticateContext(string authenticationScheme);

-        public bool Accepted { get; private set; }

-        public string AuthenticationScheme { get; }

-        public IDictionary<string, object> Description { get; private set; }

-        public Exception Error { get; private set; }

-        public ClaimsPrincipal Principal { get; private set; }

-        public IDictionary<string, string> Properties { get; private set; }

-        public virtual void Authenticated(ClaimsPrincipal principal, IDictionary<string, string> properties, IDictionary<string, object> description);

-        public virtual void Failed(Exception error);

-        public virtual void NotAuthenticated();

-    }
-    public enum ChallengeBehavior {
 {
-        Automatic = 0,

-        Forbidden = 2,

-        Unauthorized = 1,

-    }
-    public class ChallengeContext {
 {
-        public ChallengeContext(string authenticationScheme);

-        public ChallengeContext(string authenticationScheme, IDictionary<string, string> properties, ChallengeBehavior behavior);

-        public bool Accepted { get; private set; }

-        public string AuthenticationScheme { get; }

-        public ChallengeBehavior Behavior { get; }

-        public IDictionary<string, string> Properties { get; }

-        public void Accept();

-    }
-    public class DescribeSchemesContext {
 {
-        public DescribeSchemesContext();

-        public IEnumerable<IDictionary<string, object>> Results { get; }

-        public void Accept(IDictionary<string, object> description);

-    }
     public class HttpAuthenticationFeature : IHttpAuthenticationFeature {
-        public IAuthenticationHandler Handler { get; set; }

     }
-    public interface IAuthenticationHandler {
 {
-        Task AuthenticateAsync(AuthenticateContext context);

-        Task ChallengeAsync(ChallengeContext context);

-        void GetDescriptions(DescribeSchemesContext context);

-        Task SignInAsync(SignInContext context);

-        Task SignOutAsync(SignOutContext context);

-    }
     public interface IHttpAuthenticationFeature {
-        IAuthenticationHandler Handler { get; set; }

     }
-    public class SignInContext {
 {
-        public SignInContext(string authenticationScheme, ClaimsPrincipal principal, IDictionary<string, string> properties);

-        public bool Accepted { get; private set; }

-        public string AuthenticationScheme { get; }

-        public ClaimsPrincipal Principal { get; }

-        public IDictionary<string, string> Properties { get; }

-        public void Accept();

-    }
-    public class SignOutContext {
 {
-        public SignOutContext(string authenticationScheme, IDictionary<string, string> properties);

-        public bool Accepted { get; private set; }

-        public string AuthenticationScheme { get; }

-        public IDictionary<string, string> Properties { get; }

-        public void Accept();

-    }
 }
```

