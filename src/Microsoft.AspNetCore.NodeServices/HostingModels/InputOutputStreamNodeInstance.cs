using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.NodeServices {
    // This is just to demonstrate that other transports are possible. This implementation is extremely
    // dubious - if the Node-side code fails to conform to the expected protocol in any way (e.g., has an
    // error), then it will just hang forever. So don't use this.
    //
    // But it's fast - the communication round-trip time is about 0.2ms (tested on OS X on a recent machine),
    // versus 2-3ms for the HTTP transport.
    //
    // Instead of directly using stdin/stdout, we could use either regular sockets (TCP) or use named pipes
    // on Windows and domain sockets on Linux / OS X, but either way would need a system for framing the
    // requests, associating them with responses, and scheduling use of the comms channel.
    internal class InputOutputStreamNodeInstance : OutOfProcessNodeInstance
    {
        private SemaphoreSlim _invocationSemaphore = new SemaphoreSlim(1);
        private TaskCompletionSource<string> _currentInvocationResult;

        private readonly static JsonSerializerSettings jsonSerializerSettings =  new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

		public InputOutputStreamNodeInstance(string projectPath)
            : base(EmbeddedResourceReader.Read(typeof(InputOutputStreamNodeInstance), "/Content/Node/entrypoint-stream.js"), projectPath)
        {
		}

        public override async Task<T> Invoke<T>(NodeInvocationInfo invocationInfo) {
            await this._invocationSemaphore.WaitAsync();
            try {
                await this.EnsureReady();

                var payloadJson = JsonConvert.SerializeObject(invocationInfo, jsonSerializerSettings);
                var nodeProcess = this.NodeProcess;
                this._currentInvocationResult = new TaskCompletionSource<string>();
                nodeProcess.StandardInput.Write("\ninvoke:");
                nodeProcess.StandardInput.WriteLine(payloadJson); // WriteLineAsync isn't supported cross-platform
                var resultString = await this._currentInvocationResult.Task;
                return JsonConvert.DeserializeObject<T>(resultString);
            } finally {
                this._invocationSemaphore.Release();
                this._currentInvocationResult = null;
            }
        }

        protected override void OnOutputDataReceived(string outputData) {
            if (this._currentInvocationResult != null) {
                this._currentInvocationResult.SetResult(outputData);
            } else {
                base.OnOutputDataReceived(outputData);
            }
        }
    }
}
