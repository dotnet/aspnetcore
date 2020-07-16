This is an example application to show how to set up the CSP middleware and configure the policy, and then have our nonces propagate to the templated HTML.

## How to run
1. Change the project name to `CspMiddlewareWebSite` and change the `IIS Express` entry to `CspApplication`.
2. If you get build errors that complain about missing DLLs, try `.\build.cmd` from the repo root. If you're still getting missing DLL errors for `Microsoft.AspNetCore.Mvc`, try navingating to `src\Mvc` and then running the `.\build.cmd` there. You might have to install Node and put it on your path for the compilation of ASP.NET MVC to work.
