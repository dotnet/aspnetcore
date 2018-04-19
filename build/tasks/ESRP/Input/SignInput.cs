using System;

namespace Microsoft.Build.OOB.ESRP
{
    public class SignInput
    {
        private string _version;

        public string Version
        {
            get
            {
                if (String.IsNullOrEmpty(_version))
                {
                    _version = "1.0.0";
                }
                return _version;
            }
            set
            {
                _version = value;
            }
        }

        public SignBatches[] SignBatches
        {
            get;
            set;
        }
    }
}
