using System;

namespace Microsoft.Build.OOB.ESRP
{
    public class SignBatches
    {
        private string _sourceLocationType;
        private string _destinationLocationType;

        public string SourceLocationType
        {
            get
            {
                if (String.IsNullOrEmpty(_sourceLocationType))
                {
                    _sourceLocationType = LocationType.UNC;
                }
                return _sourceLocationType;
            }
            set
            {
                _sourceLocationType = value;
            }
        }

        public string SourceRootDirectory
        {
            get;
            set;
        }

        public string DestinationLocationType
        {
            get
            {
                if (String.IsNullOrEmpty(_destinationLocationType))
                {
                    _destinationLocationType = LocationType.UNC;
                }
                return _destinationLocationType;
            }
            set
            {
                _destinationLocationType = LocationType.UNC;
            }
        }

        public string DestinationRootDirectory
        {
            get;
            set;
        }

        public SignRequestFiles[] SignRequestFiles
        {
            get;
            set;
        }

        public SigningInfo SigningInfo
        {
            get;
            set;
        }
    }
}
