## Please use "dotnet new" templates instead

As of .NET Core 2.0, it's no longer necessary to use Yeoman to create new Single-Page Application projects.

Using the .NET Core 2.0 SDK, you can run any of the following commands in an empty directory, without needing to install any external packages first:

 * `dotnet new angular`
 * `dotnet new react`
 * `dotnet new redux`

Or, if you want to create an Aurelia, Knockout, or Vue application, you should run `dotnet new --install Microsoft.AspNetCore.SpaTemplates::*` first. This will add `aurelia`, `knockout`, and `vue` templates to `dotnet new`.

### This Yeoman generator is DEPRECATED

Please don't use `generator-aspnetcore-spa` to create new projects. Its output is outdated and no longer maintained. Instead, use `dotnet new` as described above (or if you're on Windows and use Visual Studio, you can just use *File->New Project* to create Angular, React, or React+Redux projects).
