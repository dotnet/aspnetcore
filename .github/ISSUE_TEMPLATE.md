### Please consider the following before filing an issue

* If you are using NuGet Packages that start with `Microsoft.AspNet.SignalR`, use the issue tracker at https://github.com/SignalR/SignalR to report the issue. This 
repository is for packages that start with `Microsoft.AspNetCore.SignalR` (and NPM packages that start with `@aspnet/signalr`)

### Please include as much of the following as you can in your bug report

* Versions of Server-Side NuGet Packages:
* Versions of Client-Side NuGet/NPM Packages:
* Are you using the C# client or the JavaScript client:
* The Server you are using (Kestrel/HttpSysServer/IIS/IIS Express/Azure Web App/etc.): 
* The Operating System on the Server (Windows/Linux/macOS):
* The Operating System on the Client (Windows/Linux/macOS):
* The Browser on the client, if using the JavaScript client (IE/Chrome/Edge/Firefox/etc.):
* If possible, please collect Network Traces and attach them (please do not post them inline, use a service like [Gist](https://gist.github.com) to upload them and link them in the issue)
   * For either client you can use a tool such as [Fiddler](https://www.telerik.com/fiddler) for this
   * Many browsers allow you to capture Network Traces from their Dev Tools. See sample instructions for Chrome: https://support.zendesk.com/hc/en-us/articles/204410413-Generating-a-HAR-file-for-troubleshooting
* If possible, please collect logs from the client:
   * Set the `logger` option on your `HubConnection` to `LogLevel.Trace` and find the logs in the Console tab of your Browser Dev Tools
   * Example: `new signalR.HubConnection(url, { logger: signalR.LogLevel.Trace })`
* If possible, please collect logs from the server:
   * When using Kestrel/HttpSysServer, these are available on the Console by default
   * When using IIS/IIS Express, these are available in Visual Studio in the "ASP.NET Core Web Server" section of the Output Window
   * See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?tabs=aspnetcore2x for more information

When in doubt, feel free to file the issue, we're happy to help answer questions. We also suggest using the `asp.net-core-signalr` tag on StackOverflow to ask questions.