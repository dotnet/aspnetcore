using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Net.Server
{
    [Flags]
    public enum AuthenticationType
    {
        None = 0x0,
        Basic = 0x1,
        Digest = 0x2,
        Ntlm = 0x4,
        Negotiate = 0x8,
        Kerberos = 0x10,
    }
}
