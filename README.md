Localization
==========
AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/omn0l2l3mfhfjjpp?svg=true)](https://ci.appveyor.com/project/aspnetci/Localization/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/Localization.svg?branch=dev)](https://travis-ci.org/aspnet/Localization)

| Package | [aspnet-core](https://dotnet.myget.org/gallery/aspnetcore-dev) | [NuGet](https://nuget.org) |
| ------- | ----------------------- | ----------------- |
| Microsoft.Extensions.Localization | [![MyGet](https://img.shields.io/dotnet.myget/aspnetcore-dev/vpre/Microsoft.Extensions.Localization.svg)](https://dotnet.myget.org/feed/aspnetcore-dev/package/nuget/Microsoft.Extensions.Localization) | [![NuGet](https://img.shields.io/nuget/v/Microsoft.Extensions.Localization.svg)](https://nuget.org/packages/Microsoft.Extensions.Localization) |
| Microsoft.AspNetCore.Localization | [![MyGet](https://img.shields.io/dotnet.myget/aspnetcore-dev/vpre/Microsoft.AspNetCore.Localization.svg)](https://dotnet.myget.org/feed/aspnetcore-dev/package/nuget/Microsoft.AspNetCore.Localization) | [![NuGet](https://img.shields.io/nuget/v/Microsoft.AspNetCore.Localization.svg)](https://nuget.org/packages/Microsoft.AspNetCore.Localization) |


Localization abstractions and implementations for ASP.NET Core applications.

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

### Samples

Here are a few samples that demonstrate different localization features including: localized views, localized strings in data annotations, creating custom localization resources ... etc.

 * [Localization.StarterWeb](https://github.com/aspnet/Entropy/tree/dev/samples/Localization.StarterWeb) - comprehensive localization sample demonstrates almost all of the localization features
 * [Localization.EntityFramework](https://github.com/aspnet/Entropy/tree/dev/samples/Localization.EntityFramework) - localization sample that uses an EntityFramework based localization provider for resources
 * [Localization.CustomResourceManager](https://github.com/aspnet/Entropy/tree/dev/samples/Localization.CustomResourceManager) - localization sample that uses a custom `ResourceManagerStringLocalizer`

### Providers

Community projects adapt _RequestCultureProvider_ for determining the culture information of an `HttpRequest`.

 * [My.AspNetCore.Localization.Session](https://github.com/hishamco/My.AspNetCore.Localization.Session) - determines the culture information for a request via values in the session state.