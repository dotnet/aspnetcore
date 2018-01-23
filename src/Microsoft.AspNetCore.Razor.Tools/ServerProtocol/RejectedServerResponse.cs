// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal sealed class RejectedServerResponse : ServerResponse
    {
        public override ResponseType Type => ResponseType.Rejected;

        /// <summary>
        /// RejectedResponse has no body.
        /// </summary>
        protected override void AddResponseBody(BinaryWriter writer) { }
    }
}
