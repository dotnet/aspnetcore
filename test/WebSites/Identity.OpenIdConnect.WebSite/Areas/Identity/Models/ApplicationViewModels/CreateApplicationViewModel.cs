using System.ComponentModel.DataAnnotations;

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class CreateApplicationViewModel
    {
        [Required]
        public string Name { get; set; }
    }
}
