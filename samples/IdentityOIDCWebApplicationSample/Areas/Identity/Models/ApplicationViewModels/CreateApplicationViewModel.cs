using System.ComponentModel.DataAnnotations;

namespace IdentityOIDCWebApplicationSample.Identity.Models.ApplicationViewModels
{
    public class CreateApplicationViewModel
    {
        [Required]
        public string Name { get; set; }
    }
}
