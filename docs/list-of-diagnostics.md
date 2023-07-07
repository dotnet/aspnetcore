# List of Diagnostics Produced by ASP.NET Libraries APIs

## Analyzer Warnings

### ASP  (`ASP0000-ASP0024`)

| Diagnostic ID     | Description |
| :---------------- | :---------- |
|  __`ASP0000`__ | Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices' |
|  __`ASP0001`__ | Authorization middleware is incorrectly configured |
|  __`ASP0003`__ | Do not use model binding attributes with route handlers |
|  __`ASP0004`__ | Do not use action results with route handlers |
|  __`ASP0005`__ | Do not place attribute on method called by route handler lambda |
|  __`ASP0006`__ | Do not use non-literal sequence numbers |
|  __`ASP0007`__ | Route parameter and argument optionality is mismatched |
|  __`ASP0008`__ | Do not use ConfigureWebHost with WebApplicationBuilder.Host |
|  __`ASP0009`__ | Do not use Configure with WebApplicationBuilder.WebHost |
|  __`ASP0010`__ | Do not use UseStartup with WebApplicationBuilder.WebHost |
|  __`ASP0011`__ | Suggest using builder.Logging over Host.ConfigureLogging or WebHost.ConfigureLogging |
|  __`ASP0012`__ | Suggest using builder.Services over Host.ConfigureServices or WebHost.ConfigureServices |
|  __`ASP0013`__ | Suggest switching from using Configure methods to WebApplicationBuilder.Configuration |
|  __`ASP0014`__ | Suggest using top level route registrations |
|  __`ASP0015`__ | Suggest using IHeaderDictionary properties |
|  __`ASP0016`__ | Do not return a value from RequestDelegate |
|  __`ASP0017`__ | Invalid route pattern |
|  __`ASP0018`__ | Unused route parameter |
|  __`ASP0019`__ | Suggest using IHeaderDictionary.Append or the indexer |
|  __`ASP0020`__ | Complex types referenced by route parameters must be parsable |
|  __`ASP0021`__ | When implementing BindAsync(...) method, the return type must be ValueTask&lt;T&gt; |
|  __`ASP0022`__ | Route conflict detected between route handlers |
|  __`ASP0023`__ | Route conflict detected between controller actions |
|  __`ASP0024`__ | Route handler has multiple parameters with the [FromBody] attribute |

### API (`API1000-API1003`)

| Diagnostic ID     | Description |
| :---------------- | :---------- |
|  __`API1000`__ | Action returns undeclared status code |
|  __`API1001`__ | Action returns undeclared success result |
|  __`API1002`__ | Action documents status code that is not returned |
|  __`API1003`__ | Action methods on ApiController instances do not require explicit model validation check |

### MVC (`MVC1000` - `MVC1006`)

| Diagnostic ID     | Description |
| :---------------- | :---------- |
|  __`MVC1000`__ | Use of IHtmlHelper.{0} should be avoided |
|  __`MVC1001`__ | Filters cannot be applied to page handler methods |
|  __`MVC1002`__ | Route attributes cannot be applied to page handler methods |
|  __`MVC1003`__ | Route attributes cannot be applied to page models |
|  __`MVC1004`__ | Rename model bound parameter |
|  __`MVC1005`__ | Cannot use UseMvc with Endpoint Routing |
|  __`MVC1006`__ | Methods containing TagHelpers must be async and return Task |

### BL  (`BL0001-BL0007`)

| Diagnostic ID     | Description |
| :---------------- | :---------- |
|  __`BL0001`__ | Component parameter should have public setters |
|  __`BL0002`__ | Component has multiple CaptureUnmatchedValues parameters |
|  __`BL0003`__ | Component parameter with CaptureUnmatchedValues has the wrong type |
|  __`BL0004`__ | Component parameter should be public |
|  __`BL0005`__ | Component parameter should not be set outside of its component |
|  __`BL0006`__ | Do not use RenderTree types |
|  __`BL0007`__ | Component parameters should be auto properties |

### Request Delegate Generator  (`RDG001-RDG004`)

| Diagnostic ID     | Description |
| :---------------- | :---------- |
|  __`RDG001`__ | Unable to resolve route pattern |
|  __`RDG002`__ | Unable to resolve endpoint handler |
|  __`RDG003`__ | Unable to resolve parameter |
|  __`RDG004`__ | Unable to resolve anonymous type |
|  __`RDG005`__ | Invalid abstract type |
|  __`RDG006`__ | Invalid constructor parameters |
|  __`RDG007`__ | No valid constructor found |
|  __`RDG008`__ | Multiple public constructors found |
|  __`RDG009`__ | Invalid nested AsParameters |
|  __`RDG010`__ | Unexpected nullable type |

### SignalR Source Generator (`SSG0000-SSG0110`)

| Diagnostic ID     | Description |
| :---------------- | :---------- |
|  __`SSG0000`__ | Non-interface generic type argument |
|  __`SSG0001`__ | Unsupported return type |
|  __`SSG0002`__ | Too many HubServerProxy attributed methods |
|  __`SSG0003`__ | HubServerProxy attributed method has bad accessibility |
|  __`SSG0004`__ | HubServerProxy attributed method is not partial |
|  __`SSG0005`__ | HubServerProxy attributed method is not an extension method |
|  __`SSG0006`__ | HubServerProxy attributed method has bad number of type arguments |
|  __`SSG0007`__ | HubServerProxy attributed method type argument and return type does not match |
|  __`SSG0008`__ | HubServerProxy attributed method has bad number of arguments |
|  __`SSG0009`__ | HubServerProxy attributed method has argument of wrong type |
|  __`SSG0100`__ | Unsupported return type |
|  __`SSG0102`__ | Too many HubClientProxy attributed methods |
|  __`SSG0103`__ | HubClientProxy attributed method has bad accessibility |
|  __`SSG0104`__ | HubClientProxy attributed method is not partial |
|  __`SSG0105`__ | HubClientProxy attributed method is not an extension method |
|  __`SSG0106`__ | HubClientProxy attributed method has bad number of type arguments |
|  __`SSG0107`__ | HubClientProxy attributed method type argument and return type does not match |
|  __`SSG0108`__ | HubClientProxy attributed method has bad number of arguments |
|  __`SSG0109`__ | HubClientProxy attributed method has first argument of wrong type |
|  __`SSG0110`__ | HubClientProxy attributed method has wrong return type |
