using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Twitter;
using Newtonsoft.Json;

namespace MusicStore.Mocks.Twitter
{
    /// <summary>
    /// Summary description for CustomTwitterStateDataFormat
    /// </summary>
    public class CustomTwitterStateDataFormat : ISecureDataFormat<RequestToken>
    {
        private static string _lastSavedRequestToken;

        public string Protect(RequestToken data)
        {
            data.Token = "valid_oauth_token";
            _lastSavedRequestToken = Serialize(data);
            return "valid_oauth_token";
        }

        public string Protect(RequestToken data, string purpose)
        {
            return Protect(data);
        }

        public RequestToken Unprotect(string state)
        {
            return state == "valid_oauth_token" ? DeSerialize(_lastSavedRequestToken) : null;
        }

        public RequestToken Unprotect(string state, string purpose)
        {
            return Unprotect(state);
        }

        private string Serialize(RequestToken data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        private RequestToken DeSerialize(string state)
        {
            return JsonConvert.DeserializeObject<RequestToken>(state);
        }
    }
}
