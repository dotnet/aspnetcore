using System;
using Microsoft.AspNetCore.Blazor.Browser.Interop;

namespace BlazorContent.CSharp
{
    public class ExampleJsInterop
    {
        public static string Prompt(string message)
        {
            return RegisteredFunction.Invoke<string>(
                "BlazorContent.CSharp.ExampleJsInterop.Prompt",
                message);
        }
    }
}
