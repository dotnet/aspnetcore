using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Wasm.Performance.TestApp
{
    public static class WasmMemory
    {
        [JSInvokable]
        public static long GetTotalMemory() => GC.GetTotalMemory(forceFullCollection: true);
    }
}
