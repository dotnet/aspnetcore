using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    internal class ReflectedNegotiateStateFactory : INegotiateStateFactory
    {
        public INegotiateState CreateInstance()
        {
            return new ReflectedNegotiateState();
        }
    }
}
