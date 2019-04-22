# dotnet-openapi

`dotnet-openapi` is a tool which can be used to manage OpenAPI references within your project.

## Commands

### Add Commands

#### Add Project

##### Options

| Short option | Long option | Description | Example |
|-------|------|-------|---------|
| -v|--verbose | Show verbose output. |dotnet openapi add project *-v* ../Ref/ProjRef.csproj |
| -p|--project | The project to operate on. |dotnet openapi add project *--project .\Ref.csproj* ../Ref/ProjRef.csproj |

##### Arguments

|  Argument  | Description | Example |
|-------------|-------------|---------|
| source-file | The source to create a reference from. Must be a project file. |dotnet openapi add project *../Ref/ProjRef.csproj* |

#### Add File

##### Options

| Short option| Long option| Description | Example |
|-------|------|-------|---------|
| -v|--verbose | Show verbose output. |dotnet openapi add file *-v* .\openapi.json |
| -p|--project | The project to operate on. |dotnet openapi add file *--project .\Ref.csproj* .\openapi.json |

##### Arguments

|  Argument  | Description | Example |
|-------------|-------------|---------|
| source-file | The source to create a reference from. Must be an openapi file. |dotnet openapi add file *.\openapi.json* |

#### Add URL

##### Options

| Short option| Long option| Description | Example |
|-------|------|-------------|---------|
| -v|--verbose | Show verbose output. |dotnet openapi add url *-v* <http://contoso.com/openapi.json> |
| -p|--project | The project to operate on. |dotnet openapi add url *--project .\Ref.csproj* <http://contoso.com/openapi.json> |
| -o|--output-file | The file to create a local copy of. |dotnet openapi add url <https://contoso.com/openapi.json> *--output-file myclient.json* |

##### Arguments

|  Argument  | Description | Example |
|-------------|-------------|---------|
| source-file | The source to create a reference from. Must be a URL. |dotnet openapi add url <https://contoso.com/openapi.json> |

### Remove

##### Options

| Short option| Long option| Description| Example |
|-------|------|------------|---------|
| -v|--verbose | Show verbose output. |dotnet openapi remove *-v*|
| -p|--project | The project to operate on. |dotnet openapi remove *--project .\Ref.csproj* .\openapi.json |

#### Arguments

|  Argument  | Description| Example |
| ------------|------------|---------|
| source-file | The source to remove the reference to. |dotnet openapi remove *.\openapi.json* |

### Refresh

#### Options

| Short option| Long option| Description | Example |
|-------|------|-------------|---------|
| -v|--verbose | Show verbose output. | dotnet openapi refresh *-v* <https://contoso.com/openapi.json> |
| -p|--project | The project to operate on. | dotnet openapi refresh *--project .\Ref.csproj* <https://contoso.com/openapi.json> |

#### Arguments

|  Argument  | Description | Example |
| ------------|-------------|---------|
| source-file | The URL to refresh the reference from. | dotnet openapi refresh *<https://contoso.com/openapi.json*> |
