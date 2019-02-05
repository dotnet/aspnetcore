using Newtonsoft.Json;

namespace Microsoft.Build.OOB.ESRP
{
    public class SignRequestFiles
    {
        // Optional per ESRP schema
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string CustomerCorrelationId
        {
            get;
            set;
        }

        public string SourceLocation
        {
            get;
            set;
        }

        public string DestinationLocation
        {
            get;
            set;
        }
    }
}
