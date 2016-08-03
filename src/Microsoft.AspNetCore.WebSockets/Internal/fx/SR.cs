using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    // Needed to support the WebSockets code from CoreFX.
    internal static class SR
    {
        internal static readonly string net_Websockets_AlreadyOneOutstandingOperation = nameof(net_Websockets_AlreadyOneOutstandingOperation);
        internal static readonly string net_WebSockets_Argument_InvalidMessageType = nameof(net_WebSockets_Argument_InvalidMessageType);
        internal static readonly string net_WebSockets_InvalidCharInProtocolString = nameof(net_WebSockets_InvalidCharInProtocolString);
        internal static readonly string net_WebSockets_InvalidCloseStatusCode = nameof(net_WebSockets_InvalidCloseStatusCode);
        internal static readonly string net_WebSockets_InvalidCloseStatusDescription = nameof(net_WebSockets_InvalidCloseStatusDescription);
        internal static readonly string net_WebSockets_InvalidEmptySubProtocol = nameof(net_WebSockets_InvalidEmptySubProtocol);
        internal static readonly string net_WebSockets_InvalidState = nameof(net_WebSockets_InvalidState);
        internal static readonly string net_WebSockets_InvalidState_ClosedOrAborted = nameof(net_WebSockets_InvalidState_ClosedOrAborted);
        internal static readonly string net_WebSockets_ReasonNotNull = nameof(net_WebSockets_ReasonNotNull);
        internal static readonly string net_WebSockets_UnsupportedPlatform = nameof(net_WebSockets_UnsupportedPlatform);

        internal static string Format(string name, params object[] args) => $"TODO, RESX: {name}; ({string.Join(",", args)})";
    }
}
