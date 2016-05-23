using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// This is just to demonstrate that other transports are possible. This implementation is extremely
    /// dubious - if the Node-side code fails to conform to the expected protocol in any way (e.g., has an
    /// error), then it will just hang forever. So don't use this.
    ///
    /// But it's fast - the communication round-trip time is about 0.2ms (tested on OS X on a recent machine),
    /// versus 2-3ms for the HTTP transport.
    ///
    /// Instead of directly using stdin/stdout, we could use either regular sockets (TCP) or use named pipes
    /// on Windows and domain sockets on Linux / OS X, but either way would need a system for framing the
    /// requests, associating them with responses, and scheduling use of the comms channel.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.NodeServices.OutOfProcessNodeInstance" />
    internal class InputOutputStreamNodeInstance : OutOfProcessNodeInstance
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private TaskCompletionSource<string> _currentInvocationResult;
        private readonly SemaphoreSlim _invocationSemaphore = new SemaphoreSlim(1);

        public InputOutputStreamNodeInstance(string projectPath)
            : base(
                EmbeddedResourceReader.Read(
                    typeof(InputOutputStreamNodeInstance),
                    "/Content/Node/entrypoint-stream.js"),
                projectPath)
        {
        }

        public override async Task<T> Invoke<T>(NodeInvocationInfo invocationInfo)
        {
            await _invocationSemaphore.WaitAsync();
            try
            {
                await EnsureReady();

                var payloadJson = JsonConvert.SerializeObject(invocationInfo, JsonSerializerSettings);
                var nodeProcess = NodeProcess;
                _currentInvocationResult = new TaskCompletionSource<string>();
                nodeProcess.StandardInput.Write("\ninvoke:");
                nodeProcess.StandardInput.WriteLine(payloadJson); // WriteLineAsync isn't supported cross-platform
                var resultString = await _currentInvocationResult.Task;
                return JsonConvert.DeserializeObject<T>(resultString);
            }
            finally
            {
                _invocationSemaphore.Release();
                _currentInvocationResult = null;
            }
        }

        protected override void OnOutputDataReceived(string outputData)
        {
            if (_currentInvocationResult != null)
            {
                _currentInvocationResult.SetResult(outputData);
            }
            else
            {
                base.OnOutputDataReceived(outputData);
            }
        }
    }
}