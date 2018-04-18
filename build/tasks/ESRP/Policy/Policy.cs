namespace Microsoft.Build.OOB.ESRP
{
    public class Policy
    {
        private static Policy _defaultInstance = new Policy
        {
            Intent = PolicyIntent.Flight,
            ContentType = PolicyContentType.Binaries,
            ContentOrigin = PolicyContentOrigin.FirstParty,
            ProductState = PolicyProductState.Next
        };

        public PolicyIntent Intent
        {
            get;
            set;
        }

        public PolicyContentType ContentType
        {
            get;
            set;
        }

        public PolicyContentOrigin ContentOrigin
        {
            get;
            set;
        }

        public PolicyProductState ProductState
        {
            get;
            set;
        }

        public static Policy Default
        {
            get
            {
                return _defaultInstance;
            }
        }
    }
}
