namespace Microsoft.Build.OOB.ESRP
{
    public class Auth
    {
        public string Version
        {
            get;
            set;
        }

        public string AuthenticationType
        {
            get;
            set;
        }

        public string ClientId
        {
            get;
            set;
        }

        public AuthCert AuthCert
        {
            get;
            set;
        }

        public RequestSigningCert RequestSigningCert
        {
            get;
            set;
        }
    }
}
