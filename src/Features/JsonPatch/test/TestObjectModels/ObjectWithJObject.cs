using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class ObjectWithJObject
    {
        public JObject CustomData { get; set; } = new JObject();
    }
}
