using System.ComponentModel.DataAnnotations;

namespace BlazorUnitedApp.Validation;

public class CustomerModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    public string? Email { get; set; }

    public AddressModel ShippingAddress { get; set; } = new AddressModel();
}
