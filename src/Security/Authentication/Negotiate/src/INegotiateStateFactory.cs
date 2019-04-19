using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    internal interface INegotiateStateFactory
    {
        INegotiateState CreateInstance();
    }
}
