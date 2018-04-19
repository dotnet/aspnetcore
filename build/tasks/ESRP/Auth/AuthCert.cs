using System;

namespace Microsoft.Build.OOB.ESRP
{
    public class AuthCert
    {
        public string SubjectName
        {
            get;
            set;
        }

        public string StoreLocation
        {
            get;
            set;
        }

        public string StoreName
        {
            get;
            set;
        }

        public static AuthCert Create(string applicationId)
        {
            return new AuthCert { SubjectName = String.Format("{0}.microsoft.com", applicationId), StoreLocation = "LocalMachine", StoreName = "My" };
        }
    }
}
