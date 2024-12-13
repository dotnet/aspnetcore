## About

`Microsoft.Extensions.Logging.AzureAppServices` provides a logger implementation that logs to text files in an Azure App Service app's file system and to blob storage in an Azure Storage account.

## Key Features

* Loging messages with the "Diagnostics Logger" and "Log Streaming" features of Azure App Service
* Integration with Azure App Service logging infrastructure
* Seamless integration with other components of `Microsoft.Extensions.Logging`

## How to Use

To use `Microsoft.Extensions.Logging.AzureAppServices`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.Extensions.Logging.AzureAppServices
```

### Configuration

To configure provider settings, use `AzureFileLoggerOptions` and `AzureBlobLoggerOptions`, as shown in the following example:

```csharp
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder();
builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "azure-diagnostics-";
    options.FileSizeLimit = 50 * 1024;
    options.RetainedFileCountLimit = 5;
});
builder.Services.Configure<AzureBlobLoggerOptions>(options =>
{
    options.BlobName = "log.txt";
});
```

## Main Types

* `FileLoggerProvider`: A `BatchingLoggerProvider` which writes out to a file
* `BlobLoggerProvider`: The `ILoggerProvider` implementation that stores messages by appending them to Azure Blob in batches
* `AzureFileLoggerOptions`: Options for configuring Azure diagnostics file logging
* `AzureBlobLoggerOptions`: Options for configuring Azure diagnostics blob logging

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/fundamentals/logging#azure-app-service) on logging with ASP.NEt Core and Azure App Service.

## Feedback &amp; Contributing

`Microsoft.Extensions.Logging.AzureAppServices` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
