using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Wasm.Authentication.Client
{
    public class RemoteAppState : RemoteAuthenticationState
    {
        public string State { get; set; }
    }
}
