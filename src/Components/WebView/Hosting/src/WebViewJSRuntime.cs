using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView
{
    internal class WebViewJSRuntime : JSRuntime
    {
        private readonly IpcSender _ipcSender;

        public WebViewJSRuntime(IpcSender host)
        {
            JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter(new WebElementReferenceContext(this)));
            _ipcSender = host;
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            _ipcSender.BeginInvokeJS(taskId, identifier, argsJson, resultType, targetInstanceId);
        }

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            if (!invocationResult.Success)
            {
                EndInvokeDotNetCore(invocationInfo.CallId, success: false, invocationResult.Exception.ToString());
            }
            else
            {
                EndInvokeDotNetCore(invocationInfo.CallId, success: true, invocationResult.Result);
            }

            void EndInvokeDotNetCore(string callId, bool success, object resultOrError)
            {
                _ipcSender.EndInvokeDotNet(callId, success, JsonSerializer.Serialize(resultOrError, JsonSerializerOptions));
            }
        }
    }
}
