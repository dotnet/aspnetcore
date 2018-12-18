// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal sealed class ShutdownServerResponse : ServerResponse
    {
        public readonly int ServerProcessId;

        public ShutdownServerResponse(int serverProcessId)
        {
            ServerProcessId = serverProcessId;
        }

        public override ResponseType Type => ResponseType.Shutdown;

        protected override void AddResponseBody(BinaryWriter writer)
        {
            writer.Write(ServerProcessId);
        }

        public static ShutdownServerResponse Create(BinaryReader reader)
        {
            var serverProcessId = reader.ReadInt32();
            return new ShutdownServerResponse(serverProcessId);
        }
    }
}
