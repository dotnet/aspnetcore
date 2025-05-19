using System.ComponentModel.DataAnnotations;

namespace BlazorUnitedApp.Validation;

public class AddressModel
{
    [Required(ErrorMessage = "Street is required.")]
    public string? Street { get; set; }

    [Required(ErrorMessage = "Zip Code is required.")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "Zip Code must be between 5 and 10 characters.")]
    public string? ZipCode { get; set; }
}
