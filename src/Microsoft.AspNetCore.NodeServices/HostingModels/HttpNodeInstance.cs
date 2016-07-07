using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    /// <summary>
    /// A specialisation of the OutOfProcessNodeInstance base class that uses HTTP to perform RPC invocations.
    ///
    /// The Node child process starts an HTTP listener on an arbitrary available port (except where a nonzero
    /// port number is specified as a constructor parameter), and signals which port was selected using the same
    /// input/output-based mechanism that the base class uses to determine when the child process is ready to
    /// accept RPC invocations.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.NodeServices.HostingModels.OutOfProcessNodeInstance" />
    internal class HttpNodeInstance : OutOfProcessNodeInstance
    {
        private static readonly Regex PortMessageRegex =
            new Regex(@"^\[Microsoft.AspNetCore.NodeServices.HttpNodeHost:Listening on port (\d+)\]$");

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

	    private readonly HttpClient _client;
        private bool _disposed;
        private int _portNumber;

        public HttpNodeInstance(string projectPath, string[] watchFileExtensions, int port = 0)
            : base(
                EmbeddedResourceReader.Read(
                    typeof(HttpNodeInstance),
                    "/Content/Node/entrypoint-http.js"),
                projectPath,
                watchFileExtensions,
                MakeCommandLineOptions(port))
        {
            _client = new HttpClient();
		}

        private static string MakeCommandLineOptions(int port)
        {
            return $"--port {port}";
        }

        protected override async Task<T> InvokeExportAsync<T>(NodeInvocationInfo invocationInfo)
        {
            var payloadJson = JsonConvert.SerializeObject(invocationInfo, JsonSerializerSettings);
            var payload = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("http://localhost:" + _portNumber, payload);

            if (!response.IsSuccessStatusCode)
            {
                var responseErrorString = await response.Content.ReadAsStringAsync();
                throw new Exception("Call to Node module failed with error: " + responseErrorString);
            }

            var responseContentType = response.Content.Headers.ContentType;
            switch (responseContentType.MediaType)
            {
                case "text/plain":
                    // String responses can skip JSON encoding/decoding
                    if (typeof(T) != typeof(string))
                    {
                        throw new ArgumentException(
                            "Node module responded with non-JSON string. This cannot be converted to the requested generic type: " +
                            typeof(T).FullName);
                    }

                    var responseString = await response.Content.ReadAsStringAsync();
                    return (T)(object)responseString;

                case "application/json":
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(responseJson);

                case "application/octet-stream":
                    // Streamed responses have to be received as System.IO.Stream instances
                    if (typeof(T) != typeof(Stream))
                    {
                        throw new ArgumentException(
                            "Node module responded with binary stream. This cannot be converted to the requested generic type: " +
                            typeof(T).FullName + ". Instead you must use the generic type System.IO.Stream.");
                    }

                    return (T)(object)(await response.Content.ReadAsStreamAsync());

                default:
                    throw new InvalidOperationException("Unexpected response content type: " + responseContentType.MediaType);
            }
        }

        protected override void OnOutputDataReceived(string outputData)
        {
            // Watch for "port selected" messages, and when observed, store the port number
            // so we can use it when making HTTP requests. The child process will always send
            // one of these messages before it sends a "ready for connections" message.
            var match = _portNumber != 0 ? null : PortMessageRegex.Match(outputData);
            if (match != null && match.Success)
            {
                _portNumber = int.Parse(match.Groups[1].Captures[0].Value);
            }
            else
            {
                base.OnOutputDataReceived(outputData);
            }
        }

	    protected override void Dispose(bool disposing) {
	        base.Dispose(disposing);

	        if (!_disposed)
            {
	            if (disposing)
                {
	                _client.Dispose();
	            }

	            _disposed = true;
	        }
	    }
	}
}