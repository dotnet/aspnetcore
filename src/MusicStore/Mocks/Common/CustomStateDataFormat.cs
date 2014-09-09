using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security;
using Newtonsoft.Json;

namespace MusicStore.Mocks.Common
{
    public class CustomStateDataFormat : ISecureDataFormat<AuthenticationProperties>
    {
        private static string lastSavedAuthenticationProperties;

        public string Protect(AuthenticationProperties data)
        {
            lastSavedAuthenticationProperties = Serialize(data);
            return "ValidStateData";
        }

        public AuthenticationProperties Unprotect(string state)
        {
            return state == "ValidStateData" ? DeSerialize(lastSavedAuthenticationProperties) : null;
        }

        private string Serialize(AuthenticationProperties data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        private AuthenticationProperties DeSerialize(string state)
        {
            return JsonConvert.DeserializeObject<AuthenticationProperties>(state);
        }
    }
}