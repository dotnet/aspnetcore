using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite.Models
{
    public class InvalidModel : IValidatableObject
    {
        [Required]
        public string Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield return new ValidationResult("The model is not valid.");
        }
    }
}
