namespace IdentityOIDCWebApplicationSample.Identity.Models.ApplicationViewModels
{
    public class RemoveRedirectUriViewModel
    {
        public RemoveRedirectUriViewModel(string name, string redirectUri)
        {
            Name = name;
            RedirectUri = redirectUri;
        }

        public string Name { get; }
        public string RedirectUri { get; }
    }
}
