using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.NodeServices {
	internal class HttpNodeInstance : OutOfProcessNodeInstance {
        private readonly static Regex PortMessageRegex = new Regex(@"^\[Microsoft.AspNetCore.NodeServices.HttpNodeHost:Listening on port (\d+)\]$");

		private readonly static JsonSerializerSettings jsonSerializerSettings =  new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private int _portNumber;

		public HttpNodeInstance(string projectPath, int port = 0, string[] watchFileExtensions = null)
            : base(EmbeddedResourceReader.Read(typeof(HttpNodeInstance), "/Content/Node/entrypoint-http.js"), projectPath, MakeCommandLineOptions(port, watchFileExtensions))
        {
		}

        private static string MakeCommandLineOptions(int port, string[] watchFileExtensions) {
            var result = "--port " + port.ToString();
            if (watchFileExtensions != null && watchFileExtensions.Length > 0) {
                result += " --watch " + string.Join(",", watchFileExtensions);
            }
            return result;
        }

        public override async Task<T> Invoke<T>(NodeInvocationInfo invocationInfo) {
            await this.EnsureReady();

            using (var client = new HttpClient()) {
                // TODO: Use System.Net.Http.Formatting (PostAsJsonAsync etc.)
                var payloadJson = JsonConvert.SerializeObject(invocationInfo, jsonSerializerSettings);
                var payload = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("http://localhost:" + this._portNumber, payload);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) {
                    throw new Exception("Call to Node module failed with error: " + responseString);
                }

                var responseIsJson = response.Content.Headers.ContentType.MediaType == "application/json";
                if (responseIsJson) {
                    return JsonConvert.DeserializeObject<T>(responseString);
                } else if (typeof(T) != typeof(string)) {
                    throw new System.ArgumentException("Node module responded with non-JSON string. This cannot be converted to the requested generic type: " + typeof(T).FullName);
                } else {
                    return (T)(object)responseString;
                }
            }
        }

        protected override void OnOutputDataReceived(string outputData) {
            var match = this._portNumber != 0 ? null : PortMessageRegex.Match(outputData);
            if (match != null && match.Success) {
                this._portNumber = int.Parse(match.Groups[1].Captures[0].Value);
            } else {
                base.OnOutputDataReceived(outputData);
            }
        }

        protected override void OnBeforeLaunchProcess() {
            // Prepare to receive a new port number
            this._portNumber = 0;
        }
    }
}
