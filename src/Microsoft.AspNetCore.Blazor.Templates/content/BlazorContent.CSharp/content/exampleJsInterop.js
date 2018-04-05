// This file is to show how a content package may provide JavaScript interop features
// wrapped in a .NET API

Blazor.registerFunction('BlazorContent.CSharp.ExampleJsInterop.Prompt', function (message) {
    return prompt(message, 'Type anything here');
});
