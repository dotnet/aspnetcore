#if TESTING
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Http.Authentication;
using Newtonsoft.Json;

namespace MusicStore.Mocks.Common
{
    public class CustomStateDataFormat : ISecureDataFormat<AuthenticationProperties>
    {
        private static string _lastSavedAuthenticationProperties;

        public string Protect(AuthenticationProperties data)
        {
            _lastSavedAuthenticationProperties = Serialize(data);
            return "ValidStateData";
        }

        public AuthenticationProperties Unprotect(string state)
        {
            return state == "ValidStateData" ? DeSerialize(_lastSavedAuthenticationProperties) : null;
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
#endif