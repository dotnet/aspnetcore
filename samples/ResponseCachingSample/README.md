ASP.NET Core Response Caching Sample
===================================

This sample illustrates the usage of ASP.NET Core response caching middleware. The application sends a `Hello World!` message and the current time along with a `Cache-Control` header to configure caching behavior. The application also sends a `Vary` header to configure the cache to serve the response only if the `Accept-Encoding` header of subsequent requests matches that from the original request.

When running the sample, a response will be served from cache when possible and will be stored for up to 10 seconds.
