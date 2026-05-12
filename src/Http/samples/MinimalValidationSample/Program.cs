// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;
using MinimalValidationSample.Localization;

var builder = WebApplication.CreateBuilder(args);

// Validation pipeline.
builder.Services.AddValidation();

// Localization wiring:
//   * AddLocalization tells the BCL where to find resource files.
//   * AddValidationLocalization<TResource> binds every validation lookup to the shared
//     resource scope identified by SharedValidationMessages. This is the recommended
//     pattern for Minimal API parameter validation, where there is no declaring type.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddValidationLocalization<SharedValidationMessages>();

// Drive the active culture per request from the ?culture=fr|de|en query string (or
// Accept-Language header). The validation localizer reads CultureInfo.CurrentUICulture
// at message-resolution time.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = ["en", "fr", "de"];
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

app.UseRequestLocalization();

// Landing page that explains how to drive the demo.
app.MapGet("/", () => TypedResults.Text(
    """
    MinimalValidationSample — localized validation demo

    Try the same payload with different cultures via ?culture=en|fr|de or the
    Accept-Language header. The validation messages and field names switch.

    Endpoints:
      GET  /customers/{id}?culture=fr        — parameter validation
      POST /customers?culture=de             — property validation on a complex type
      POST /orders?culture=fr                — IValidatableObject + property validation
      POST /products?culture=de              — custom attribute on a parameter

    Example (PowerShell):
      curl 'http://localhost:5253/customers/0?culture=fr'
      curl -X POST 'http://localhost:5253/customers?culture=de' -H 'Content-Type: application/json' -d '{}'
    """,
    contentType: "text/plain"));

// Parameter-level validation. The display name "Param.CustomerId" is the literal
// localization key — DefaultValidationLocalizer looks it up in the shared resource.
app.MapGet("/customers/{id}",
    ([Range(1, int.MaxValue, ErrorMessage = "Validation.Range")]
     [Display(Name = "Param.CustomerId")] int id) =>
        $"Getting customer with ID: {id}");

app.MapPost("/customers", (Customer customer) =>
    TypedResults.Created($"/customers/{customer.Name}", customer));

app.MapPost("/orders", (Order order) =>
    TypedResults.Created($"/orders/{order.OrderId}", order));

// Demonstrates a custom validation attribute participating in localization through
// the standard ErrorMessage-as-key pipeline.
app.MapPost("/products",
    ([EvenNumber(ErrorMessage = "Validation.EvenNumber")]
     [Display(Name = "Param.ProductId")] int productId,
     [Required(ErrorMessage = "Validation.Required")]
     [Display(Name = "Param.ProductName")] string name) =>
        TypedResults.Ok(new { productId, name }));

app.Run();

// Validatable types with the [ValidatableType] attribute. Each property carries:
//   * [Display(Name = "<key>")] so the display name is itself localized;
//   * [<Validator>(ErrorMessage = "<key>")] so the error message template is localized.
// The default localizer looks both keys up against the shared resource source via
// IStringLocalizer, then formats the localized template with the localized display name.
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates.
[ValidatableType]
#pragma warning restore ASP0029
public class Customer
{
    [Required(ErrorMessage = "Validation.Required")]
    [Display(Name = "Customer.Name")]
    public required string Name { get; set; }

    [EmailAddress(ErrorMessage = "Validation.Email")]
    [Display(Name = "Customer.Email")]
    public required string Email { get; set; }

    [Range(18, 120, ErrorMessage = "Validation.Range")]
    [Display(Name = "Customer.Age")]
    public int Age { get; set; }

    public Address HomeAddress { get; set; } = new Address
    {
        Street = "123 Main St",
        City = "Anytown",
        ZipCode = "12345",
    };
}

public class Address
{
    [Required(ErrorMessage = "Validation.Required")]
    [Display(Name = "Address.Street")]
    public required string Street { get; set; }

    [Required(ErrorMessage = "Validation.Required")]
    [Display(Name = "Address.City")]
    public required string City { get; set; }

    [StringLength(5, ErrorMessage = "Validation.StringLength")]
    [Display(Name = "Address.Zip")]
    public required string ZipCode { get; set; }
}

// IValidatableObject for cross-property validation. Per the design, messages produced
// by Validate() are NOT routed through IValidationLocalizer — implementations are
// expected to localize manually if needed (e.g. by resolving IStringLocalizer from
// ValidationContext.GetService).
public class Order : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Validation.Range")]
    [Display(Name = "Order.Id")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Validation.Required")]
    [Display(Name = "Order.ProductName")]
    public required string ProductName { get; set; }

    [Display(Name = "Order.Quantity")]
    public int Quantity { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity <= 0)
        {
            // Resolve IStringLocalizer manually for IValidatableObject messages, since
            // the localization pipeline does not post-process them. Any localization
            // backend reachable through DI works here.
            var localizer = validationContext.GetService(
                typeof(Microsoft.Extensions.Localization.IStringLocalizer<SharedValidationMessages>))
                as Microsoft.Extensions.Localization.IStringLocalizer<SharedValidationMessages>;

            var message = localizer is null
                ? "Order quantity must be greater than zero."
                : localizer["Validation.OrderConsistency"].Value;

            yield return new ValidationResult(message, [nameof(Quantity)]);
        }
    }
}

// Custom validation attribute. Localization works automatically as long as the
// ErrorMessage value is used as the IStringLocalizer lookup key (the default
// pipeline behavior). Note: this attribute does not contribute additional template
// arguments beyond {0}, so no IValidationAttributeFormatter is needed.
public class EvenNumberAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is int number)
        {
            return number % 2 == 0;
        }
        return false;
    }
}

