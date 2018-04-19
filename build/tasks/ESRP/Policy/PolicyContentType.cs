using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Build.OOB.ESRP
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PolicyContentType
    {
        Binaries,
        App,
        Game,
        Driver,
        Document,
        [EnumMember(Value = "Source Code")]
        SourceCode
    }
}
