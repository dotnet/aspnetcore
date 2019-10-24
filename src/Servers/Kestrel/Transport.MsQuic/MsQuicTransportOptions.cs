using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic
{
    public class MsQuicTransportOptions
    {
        public ushort MaxBidirectionalStreamCount { get; set; } = 100;
        public ushort MaxUnidirectionalStreamCount { get; set; } = 10;
        public string Alpn { get; set; } = "h3-23";
        public string RegistrationName { get; set; } = "Quic";

        // TODO figure out how to make this fluent with new bedrock infrastructure
        public X509Certificate2 Certificate { get; set; }

        public TimeSpan IdleTimeout { get; set; } = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(30); // TODO think about these limits.
    }
}
