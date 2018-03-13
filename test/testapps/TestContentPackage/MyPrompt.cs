using Microsoft.AspNetCore.Blazor.Browser.Interop;

namespace TestContentPackage
{
    public static class MyPrompt
    {
        // Keep in sync with the identifier in the .js file
        const string ShowPromptIdentifier = "TestContentPackage.showPrompt";

        public static string Show(string message)
        {
            return RegisteredFunction.Invoke<string>(
                ShowPromptIdentifier,
                message);
        }
    }
}
