using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FormatterWebSite.Models
{
    public class Manager : Employee, IValidatableObject
    {
        [Required]
        public List<Employee> DirectReports { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!DirectReports.Any(e => e.Id > 20))
            {
                yield return new ValidationResult("A manager must have at least one direct report whose Id is greater than 20.");
            }
        }
    }
}
