// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebSockets
{
    internal static class WebSocketDeflateConstants
    {
        /// <summary>
        /// The maximum length that this extension can have, assuming that we're not using extra white space.
        /// <para />
        /// "permessage-deflate; client_max_window_bits=15; client_no_context_takeover; server_max_window_bits=15; server_no_context_takeover"
        /// </summary>
        public const int MaxExtensionLength = 128;

        public const string Extension = "permessage-deflate";

        public const string ClientMaxWindowBits = "client_max_window_bits";
        public const string ClientNoContextTakeover = "client_no_context_takeover";

        public const string ServerMaxWindowBits = "server_max_window_bits";
        public const string ServerNoContextTakeover = "server_no_context_takeover";
    }
}
