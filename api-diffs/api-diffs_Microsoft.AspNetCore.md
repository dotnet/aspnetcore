# Microsoft.AspNetCore

``` diff
 namespace Microsoft.AspNetCore {
     public static class WebHost {
         public static IWebHostBuilder CreateDefaultBuilder();
         public static IWebHostBuilder CreateDefaultBuilder(string[] args);
         public static IWebHostBuilder CreateDefaultBuilder<TStartup>(string[] args) where TStartup : class;
         public static IWebHost Start(RequestDelegate app);
         public static IWebHost Start(Action<IRouteBuilder> routeBuilder);
         public static IWebHost Start(string url, RequestDelegate app);
         public static IWebHost Start(string url, Action<IRouteBuilder> routeBuilder);
         public static IWebHost StartWith(Action<IApplicationBuilder> app);
         public static IWebHost StartWith(string url, Action<IApplicationBuilder> app);
     }
 }
```

