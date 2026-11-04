ASP.NET Core Output Caching Sample
===================================

This sample illustrates the usage of ASP.NET Core output caching middleware. The application sends a `Hello World!` message and the current time. A different cache entry is created for each variation of the query string.

When running the sample, a response will be served from cache when possible and will be stored for up to 10 seconds.
