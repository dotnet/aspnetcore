using System;
using Microsoft.AspNetCore.Blazor.Browser.Interop;

namespace BlazorLibrary.CSharp
{
    public class ExampleJsInterop
    {
        public static string Prompt(string message)
        {
            return RegisteredFunction.Invoke<string>(
                "BlazorLibrary.CSharp.ExampleJsInterop.Prompt",
                message);
        }
    }
}
