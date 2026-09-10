## About

`Microsoft.AspNetCore.JsonPatch` provides ASP.NET Core support for JSON PATCH requests.

## How to Use

To use `Microsoft.AspNetCore.JsonPatch`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.JsonPatch
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson
```

### Configuration

To enable JSON Patch support, call `AddNewtonsoftJson` in your ASP.NET Core app's `Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson();
```

#### Configure when using `System.Text.Json`

To add support for JSON Patch using `Newtonsoft.Json` while continuing to use `System.Text.Json` for other input and output formatters:

1. Update your `Program.cs` with logic to construct a `NewtonsoftJsonPatchInputFormatter`:
    ```csharp
    static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
    {
        var builder = new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson()
            .Services.BuildServiceProvider();

        return builder
            .GetRequiredService<IOptions<MvcOptions>>()
            .Value
            .InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>()
            .First();
    }
    ```
2. Configure the input formatter:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers(options =>
    {
        options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
    });
    ```

### Usage

To define an action method for a JSON Patch in an API controller:
1. Annotate it with the `HttpPatch` attribute
2. Accept a `JsonPatchDocument<TModel>`
3. Call `ApplyTo` on the patch document to apply changes

For example:

```csharp
[HttpPatch]
public IActionResult JsonPatchWithModelState(
    [FromBody] JsonPatchDocument<Customer> patchDoc)
{
    if (patchDoc is not null)
    {
        var customer = CreateCustomer();

        patchDoc.ApplyTo(customer, ModelState);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return new ObjectResult(customer);
    }
    else
    {
        return BadRequest(ModelState);
    }
}
```

In a real app, the code would retrieve the data from a store such as a database and update the database after applying the patch.

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/web-api/jsonpatch) on JSON Patch in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.JsonPatch` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
