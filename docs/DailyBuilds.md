How to get daily builds of ASP.NET Core
=======================================

Daily builds include the latest source code changes. They are not supported for production use and are subject to frequent changes, but we strive to make sure daily builds function correctly.

If you want to download the latest daily build and use it in a project, then you need to:

- Obtain the latest [build of the .NET Core SDK](https://github.com/dotnet/core-sdk#installers-and-binaries).
- Add a NuGet.Config to your project directory with the following content:

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="dotnet-core" value="https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json" />
          <add key="extensions" value="https://dotnetfeed.blob.core.windows.net/aspnet-extensions/index.json" />
          <add key="entityframeworkcore" value="https://dotnetfeed.blob.core.windows.net/aspnet-entityframeworkcore/index.json" />
          <add key="entityframework6" value="https://dotnetfeed.blob.core.windows.net/aspnet-entityframework6/index.json" />
          <add key="aspnetcore-tooling" value="https://dotnetfeed.blob.core.windows.net/aspnet-aspnetcore-tooling/index.json" />
          <add key="aspnetcore" value="https://dotnetfeed.blob.core.windows.net/aspnet-aspnetcore/index.json" />
          <add key="aspnet-blazor" value="https://dotnetfeed.blob.core.windows.net/aspnet-blazor/index.json" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```

  *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*

Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).

#### To debug daily builds using Visual Studio

* *Enable Source Link support* in Visual Studio should be enabled.
* *Enable source server support* in Visual should be enabled.
* *Enable Just My Code* should be disabled
* Under Symbols enable the *Microsoft Symbol Servers* setting.
