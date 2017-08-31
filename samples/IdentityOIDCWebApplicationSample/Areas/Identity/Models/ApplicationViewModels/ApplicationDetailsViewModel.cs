using System.Collections.Generic;
using System.Linq;

namespace IdentityOIDCWebApplicationSample.Identity.Models.ApplicationViewModels
{
    public class ApplicationDetailsViewModel
    {
        public string Name { get; set; }
        public string ClientId { get; set; }
        public bool HasClientSecret { get; set; }
        public IEnumerable<string> RedirectUris { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> LogoutUris { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> Scopes { get; set; } = Enumerable.Empty<string>();
    }
}
