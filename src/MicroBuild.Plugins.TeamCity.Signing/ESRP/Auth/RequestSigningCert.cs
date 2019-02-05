using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.OOB.ESRP
{
    public class RequestSigningCert
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

        public static RequestSigningCert Create(string applicationId)
        {
            return new RequestSigningCert { SubjectName = String.Format("{0}", applicationId), StoreLocation = "LocalMachine", StoreName = "My" };
        }
    }
}
