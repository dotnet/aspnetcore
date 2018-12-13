# How to regenerate the error page
* Go to the Razor Generator project
* dotnet build
* run `dotnet .\bin\Debug\netcoreapp2.1\dotnet-razorpagegenerator.dll "Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views" "<<PathToAspNetCore>>\src\Middleware\Diagnostics.EntityFrameworkCore\src\"`