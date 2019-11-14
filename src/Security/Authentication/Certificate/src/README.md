# Microsoft.AspNetCore.Authentication.Certificate

This project sort of contains an implementation of [Certificate Authentication](https://tools.ietf.org/html/rfc5246#section-7.4.4) for ASP.NET Core. Certificate authentication happens at the TLS level, long before it ever gets to ASP.NET Core, so, more accurately this is an authentication handler that validates the certificate and then gives you an event where you can resolve that certificate to a ClaimsPrincipal.

For more information, see [Configure certificate authentication in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authentication/certauth).
