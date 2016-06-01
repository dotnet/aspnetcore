using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices.HostingModels.PhysicalConnections;
using Microsoft.AspNetCore.NodeServices.HostingModels.VirtualConnections;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.NodeServices
{
    internal class SocketNodeInstance : OutOfProcessNodeInstance
    {
        private readonly static JsonSerializerSettings jsonSerializerSettings =  new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private string _addressForNextConnection;
        private readonly SemaphoreSlim _clientModificationSemaphore = new SemaphoreSlim(1);
        private StreamConnection _currentPhysicalConnection;
        private VirtualConnectionClient _currentVirtualConnectionClient;
        private readonly string[] _watchFileExtensions;

        public SocketNodeInstance(string projectPath, string[] watchFileExtensions = null): base(
                EmbeddedResourceReader.Read(
                    typeof(SocketNodeInstance),
                    "/Content/Node/entrypoint-socket.js"),
                projectPath)
        {
            _watchFileExtensions = watchFileExtensions;
		}

        public override async Task<T> Invoke<T>(NodeInvocationInfo invocationInfo)
        {
            await EnsureReady();
            var virtualConnectionClient = await GetOrCreateVirtualConnectionClientAsync();

            using (var virtualConnection = _currentVirtualConnectionClient.OpenVirtualConnection())
            {
                // Send request
                await WriteJsonLineAsync(virtualConnection, invocationInfo);

                // Receive response
                var response = await ReadJsonAsync<RpcResponse<T>>(virtualConnection);
                if (response.ErrorMessage != null)
                {
                    throw new NodeInvocationException(response.ErrorMessage, response.ErrorDetails);
                }

                return response.Result;
            }
        }

        private async Task<VirtualConnectionClient> GetOrCreateVirtualConnectionClientAsync()
        {
            var client = _currentVirtualConnectionClient;
            if (client == null)
            {
                await _clientModificationSemaphore.WaitAsync();
                try
                {
                    if (_currentVirtualConnectionClient == null)
                    {
                        var address = _addressForNextConnection;
                        if (string.IsNullOrEmpty(address))
                        {
                            // This shouldn't happen, because we always await 'EnsureReady' before getting here.
                            throw new InvalidOperationException("Cannot open connection to Node process until it has signalled that it is ready");
                        }

                        _currentPhysicalConnection = StreamConnection.Create();

                        var connection = await _currentPhysicalConnection.Open(address);
                        _currentVirtualConnectionClient = new VirtualConnectionClient(connection);
                        _currentVirtualConnectionClient.OnError += (ex) =>
                        {
                            // TODO: Log the exception properly. Need to change the chain of calls up to this point to supply
                            // an ILogger or IServiceProvider etc.
                            Console.WriteLine(ex.Message);
                            ExitNodeProcess(); // We'll restart it next time there's a request to it
                        };
                    }

                    return _currentVirtualConnectionClient;
                }
                finally
                {
                    _clientModificationSemaphore.Release();
                }
            }
            else
            {
                return client;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                EnsurePipeRpcClientDisposed();
            }

            base.Dispose(disposing);
        }

        protected override void OnBeforeLaunchProcess()
        {
            // Either we've never yet launched the Node process, or we did but the old one died.
            // Stop waiting for any outstanding requests and prepare to launch the new process.
            EnsurePipeRpcClientDisposed();
            _addressForNextConnection = "pni-" + Guid.NewGuid().ToString("D"); // Arbitrary non-clashing string
            CommandLineArguments = MakeNewCommandLineOptions(_addressForNextConnection, _watchFileExtensions);
        }

        private static async Task WriteJsonLineAsync(Stream stream, object serializableObject)
        {
            var json = JsonConvert.SerializeObject(serializableObject, jsonSerializerSettings);
            var bytes = Encoding.UTF8.GetBytes(json + '\n');
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private static async Task<T> ReadJsonAsync<T>(Stream stream)
        {
            var json = Encoding.UTF8.GetString(await ReadAllBytesAsync(stream));
            return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings);
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream input)
        {
            byte[] buffer = new byte[16*1024];

            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

        private static string MakeNewCommandLineOptions(string pipeName, string[] watchFileExtensions)
        {
            var result = "--pipename " + pipeName;
            if (watchFileExtensions != null && watchFileExtensions.Length > 0)
            {
                result += " --watch " + string.Join(",", watchFileExtensions);
            }

            return result;
        }

        private void EnsurePipeRpcClientDisposed()
        {
            _clientModificationSemaphore.Wait();

            try
            {
                if (_currentVirtualConnectionClient != null)
                {
                    _currentVirtualConnectionClient.Dispose();
                    _currentVirtualConnectionClient = null;
                }

                if (_currentPhysicalConnection != null)
                {
                    _currentPhysicalConnection.Dispose();
                    _currentPhysicalConnection = null;
                }
            }
            finally
            {
                _clientModificationSemaphore.Release();
            }
        }

#pragma warning disable 649 // These properties are populated via JSON deserialization
        private class RpcResponse<TResult>
        {
            public TResult Result { get; set; }
            public string ErrorMessage { get; set; }
            public string ErrorDetails { get; set; }
        }
#pragma warning restore 649
    }
}