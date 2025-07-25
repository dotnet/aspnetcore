// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

var builder = WebApplication.CreateSlimBuilder();

builder.Services.AddValidation();
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

// Validation endpoints with different validatable types
app.MapGet("/customers/{id}", ([Range(1, int.MaxValue)] int id) =>
    $"Getting customer with ID: {id}");

app.MapPost("/customers", (Customer customer) =>
    TypedResults.Created($"/customers/{customer.Id}", customer));

app.MapPost("/orders", (Order order) =>
    TypedResults.Created($"/orders/{order.OrderId}", order));

app.MapPost("/products", (Product product) =>
    TypedResults.Ok(new ProductResponse("Product created", product)));

app.MapPut("/addresses/{id}", (int id, Address address) =>
    TypedResults.Ok(new AddressResponse($"Address {id} updated", address)));

app.MapPost("/users", (User user) =>
    TypedResults.Created($"/users/{user.Id}", user));

app.MapPost("/inventory", ([EvenNumber] int productId, [Required] string name) =>
    TypedResults.Ok(new InventoryResponse(productId, name)));

// Endpoint with disabled validation
app.MapPost("/products/bulk", (Product[] products) =>
    TypedResults.Ok(new BulkProductResponse("Bulk products created", products.Length)))
    .DisableValidation();

await app.StartAsync().ConfigureAwait(false);

try
{
    // Create an HTTP client to test the endpoints
    using var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("http://localhost:5000");

    // Test 1: Valid ID - should succeed
    var response = await httpClient.GetAsync("/customers/123").ConfigureAwait(false);

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Console.WriteLine($"Test 1 Success: {content}");
    }
    else
    {
        Console.WriteLine($"Test 1 Failed: {response.StatusCode}");
        return 1;
    }

    // Test 2: Invalid ID (0) - should fail validation
    try
    {
        await httpClient.GetAsync("/customers/0").ConfigureAwait(false);
    }
    catch (HttpRequestException httpEx)
    {
        // Handle HttpRequestException and assert on the response
        if (httpEx.Data.Contains("HttpResponse"))
        {
            var invalidResponse = (HttpResponseMessage)httpEx.Data["HttpResponse"];
            if (invalidResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorContent = await invalidResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine($"Test 2 Success (Caught HttpRequestException with validation error): {invalidResponse.StatusCode}");
                Console.WriteLine($"Error content: {errorContent}");
            }
            else
            {
                Console.WriteLine($"Test 2 Failed: Expected BadRequest in exception but got {invalidResponse.StatusCode}");
                return 1;
            }
        }
        else
        {
            Console.WriteLine($"Test 2 Failed: HttpRequestException without HttpResponse data: {httpEx.Message}");
            return 1;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
finally
{
    await app.StopAsync().ConfigureAwait(false);
}

return 100;

public class Customer
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(18, 120)]
    [Display(Name = "Customer Age")]
    public int Age { get; set; }

    // Complex property with nested validation
    public Address Address { get; set; } = new();
}

public record Product(
    [Required] string Name,
    [Range(0.01, double.MaxValue)] decimal Price,
    [StringLength(500)] string Description = ""
)
{
    [Required]
    public string Category { get; set; } = string.Empty;
}

public class Address
{
    [Required]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code format")]
    public string ZipCode { get; set; } = string.Empty;

    [StringLength(2, MinimumLength = 2)]
    public string State { get; set; } = string.Empty;
}

public class Order : IValidatableObject
{
    [Required]
    public string OrderId { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Total { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderDate > DateTime.Now.AddDays(1))
        {
            yield return new ValidationResult(
                "Order date cannot be more than 1 day in the future.",
                new[] { nameof(OrderDate) });
        }

        if (Total > 10000 && CustomerId == 0)
        {
            yield return new ValidationResult(
                "High-value orders require a valid customer ID.",
                new[] { nameof(CustomerId), nameof(Total) });
        }
    }
}

public record User(
    [Required] [StringLength(50)] string Username,
    [EmailAddress] string Email,
    [Phone] string PhoneNumber = ""
)
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [StringLength(100)]
    public string DisplayName => $"{Username} ({Email})";

    [CreditCard]
    public string CreditCardNumber { get; set; }
}

// Custom validation attribute
public class EvenNumberAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is int number && number % 2 != 0)
        {
            return new ValidationResult("The number must be even.", new[] { validationContext.MemberName });
        }

        return ValidationResult.Success;
    }
}

public record ProductResponse(string Message, Product Product);

public record AddressResponse(string Message, Address Address);

public record InventoryResponse(int ProductId, string Name);

public record BulkProductResponse(string Message, int Count);

[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(ProductResponse))]
[JsonSerializable(typeof(AddressResponse))]
[JsonSerializable(typeof(InventoryResponse))]
[JsonSerializable(typeof(BulkProductResponse))]
[JsonSerializable(typeof(Product[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Http.HttpValidationProblemDetails))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
{
}
