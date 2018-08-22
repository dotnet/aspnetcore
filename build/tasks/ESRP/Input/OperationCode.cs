using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Build.OOB.ESRP
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperationCode
    {
        Default,
        NuGetSign,
        NuGetVerify,
        OpcSign,
        OpcVerify,
        SigntoolSign,
        SigntoolVerify,
        StrongNameSign,
        StrongNameVerify,
        JavaSign,
        JavaVerify,
    }
}
