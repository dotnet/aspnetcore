using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class JsonPropertyWithInheritanceDTO : JsonPropertyWithInheritanceBaseDTO
    {
        public override string Name { get; set; }
    }

    public abstract class JsonPropertyWithInheritanceBaseDTO
    {
        [JsonProperty("AnotherName")]
        public abstract string Name { get; set; }
    }
}
