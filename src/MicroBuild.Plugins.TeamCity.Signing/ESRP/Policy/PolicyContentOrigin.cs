using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Build.OOB.ESRP
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PolicyContentOrigin
    {
        [EnumMember(Value = "1stParty")]
        FirstParty,
        [EnumMember(Value = "2ndParty")]
        SecondParty,
        [EnumMember(Value = "3rdParty")]
        ThirdParty
    }
}
