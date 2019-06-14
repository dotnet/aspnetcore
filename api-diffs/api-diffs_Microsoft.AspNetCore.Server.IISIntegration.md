# Microsoft.AspNetCore.Server.IISIntegration

``` diff
 namespace Microsoft.AspNetCore.Server.IISIntegration {
     public class IISDefaults {
-        public static readonly string AuthenticationScheme;
+        public const string AuthenticationScheme = "Windows";
     }
     public class IISMiddleware {
-        public IISMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IISOptions> options, string pairingToken, IAuthenticationSchemeProvider authentication, IApplicationLifetime applicationLifetime);

+        public IISMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IISOptions> options, string pairingToken, IAuthenticationSchemeProvider authentication, IHostApplicationLifetime applicationLifetime);
-        public IISMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IISOptions> options, string pairingToken, bool isWebsocketsSupported, IAuthenticationSchemeProvider authentication, IApplicationLifetime applicationLifetime);

+        public IISMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IISOptions> options, string pairingToken, bool isWebsocketsSupported, IAuthenticationSchemeProvider authentication, IHostApplicationLifetime applicationLifetime);
     }
 }
```

