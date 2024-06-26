# How to get daily builds of ASP.NET Core

Daily builds include the latest source code changes. They are not supported for production use and are subject to frequent changes, but we strive to make sure daily builds function correctly.

If you want to download the latest daily build and use it in a project, then you need to:

* Obtain the latest [build of the .NET Core SDK](https://github.com/dotnet/sdk/blob/main/documentation/package-table.md).
* Add a NuGet.Config to your project directory with the following content:

## .NET 8

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="dotnet8" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```

## .NET 9

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="dotnet9" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```

  *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*

Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).

## To debug daily builds using Visual Studio

* *Enable Source Link support* in Visual Studio should be enabled.
* *Enable source server support* in Visual should be enabled.
* *Enable Just My Code* should be disabled
* Under Symbols enable the *Microsoft Symbol Servers* setting.

## Downloading daily builds of aspnetcore

Links to our daily builds can be found [here](https://github.com/dotnet/aspnetcore/blob/main/README.md#nightly-builds).
