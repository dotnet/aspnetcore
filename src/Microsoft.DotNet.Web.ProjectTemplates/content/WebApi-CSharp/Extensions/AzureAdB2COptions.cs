namespace Microsoft.AspNetCore.Authentication
{
    public class AzureAdB2COptions
    {
        public string ClientId { get; set; }
        public string Instance { get; set; }
        public string Domain { get; set; }
        public string SignUpSignInPolicyId { get; set; }
    }
}
