using System.Collections.Generic;

namespace IdentityOIDCWebApplicationSample.Identity.Models.ApplicationViewModels
{
    public class CreateScopeViewModel
    {
        public CreateScopeViewModel()
        {
        }

        public CreateScopeViewModel(string applicationName, IEnumerable<string> scopes)
        {
            Name = applicationName;
            Scopes = scopes;
        }

        public string Name { get; }
        public IEnumerable<string> Scopes { get; }
        public string NewScope { get; set; }
    }
}
