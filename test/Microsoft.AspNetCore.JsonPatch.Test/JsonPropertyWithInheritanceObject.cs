using Newtonsoft.Json;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class JsonPropertyWithInheritanceObject : JsonPropertyWithInheritanceBaseObject
    {
        public override string Name { get; set; }
    }

    public abstract class JsonPropertyWithInheritanceBaseObject
    {
        [JsonProperty("AnotherName")]
        public abstract string Name { get; set; }
    }
}
