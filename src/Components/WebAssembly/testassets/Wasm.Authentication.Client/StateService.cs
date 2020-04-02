using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wasm.Authentication.Client
{
    public class StateService
    {
        private string _state;

        public string GetCurrentState() => _state ??= Guid.NewGuid().ToString();

        public void RestoreCurrentState(string state) => _state = state;
    }
}
