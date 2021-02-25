using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    internal class WebViewJSRuntime : JSRuntime
    {
        private readonly IWebViewHost _host;

        public WebViewJSRuntime(IWebViewHost host)
        {
            _host = host;
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            _host.BeginInvokeJS(taskId, identifier, argsJson, resultType, targetInstanceId);
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
                _host.EndInvokeDotNet(callId, success, JsonSerializer.Serialize(resultOrError, JsonSerializerOptions));
            }
        }
    }
}
