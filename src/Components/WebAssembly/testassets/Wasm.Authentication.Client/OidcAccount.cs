using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Wasm.Authentication.Client
{
    public class OidcAccount : RemoteUserAccount
    {
        [JsonPropertyName("amr")]
        public string[] AuthenticationMethod { get; set; }
    }
}
