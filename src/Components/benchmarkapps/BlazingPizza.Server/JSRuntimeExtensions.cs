using Microsoft.JSInterop;

namespace BlazingPizza.Server;

public static class JSRuntimeExtensions
{
    public static ValueTask<bool> Confirm(this IJSRuntime jsRuntime, string message)
    {
        return jsRuntime.InvokeAsync<bool>("confirm", message);
    }
}
