Optional certificates sample
============================

Client certificates are relatively easy to configure when they're required for all requests, you configure it in the server bindings as required and add the auth handler to validate it. Things are much trickier when you only want to require client certificates for some parts of your application.

Client certificates are not an HTTP feature, they're a TLS feature. As such they're not included in the HTTP request structure like headers, they're negotiated when establishing the connection. This makes it impossible to require a certificate for some requests but not others on a given connection.

There's an old way to renegotiate a connection if you find you need a client cert after it's established. It's a TLS action that pauses all traffic, redoes the TLS handshake, and allows you to request a client certificate. This caused a number of problems including weakening security, TCP deadlocks for POST requests, etc.. HTTP/2 has since disallowed this mechanism.

This example shows an pattern for requiring client certificates only in some parts of your site by using different host bindings. The application is set up using two host names, example.com and cert.example.com

cert.example.com is configured in the server to require client certificates, but example.com is not. When you request part of the site that requires a client certificate it can redirect to the cert.example.com while preserving the request path and query and the client will prompt for a certificate.

Redirecting back to example.com does not accomplish a real sign-out because the browser still caches the client cert selected for cert.example.com. The only way to clear the browser cache is to close the browser.
