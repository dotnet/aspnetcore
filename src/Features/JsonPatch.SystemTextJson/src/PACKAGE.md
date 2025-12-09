## About

`Microsoft.AspNetCore.JsonPatch.SystemTextJson` provides ASP.NET Core support for JSON PATCH requests.

## How to Use

To use `Microsoft.AspNetCore.JsonPatch.SystemTextJson`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.JsonPatch.SystemTextJson
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

`Microsoft.AspNetCore.JsonPatch.SystemTextJson` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
