using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Build.OOB.ESRP
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PolicyIntent
    {
        ProductRelease,
        DevTest,
        Flight,
        MSDCDeployment,
        NonMSDCDeployment
    };    
}
