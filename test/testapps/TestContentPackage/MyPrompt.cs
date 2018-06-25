using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace TestContentPackage
{
    public static class MyPrompt
    {
        public static Task<string> Show(string message)
        {
            return JSRuntime.Current.InvokeAsync<string>(
                "TestContentPackage.showPrompt", // Keep in sync with identifiers in the.js file
                message);
        }
    }
}
