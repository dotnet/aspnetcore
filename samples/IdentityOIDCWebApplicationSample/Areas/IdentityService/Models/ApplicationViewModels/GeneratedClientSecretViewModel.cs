namespace IdentityOIDCWebApplicationSample.Identity.Models.ApplicationViewModels
{
    public class GeneratedClientSecretViewModel
    {
        public GeneratedClientSecretViewModel(string name, string clientSecret)
        {
            Name = name;
            ClientSecret = clientSecret;
        }

        public string Name { get; }
        public string ClientSecret { get; }
    }
}
