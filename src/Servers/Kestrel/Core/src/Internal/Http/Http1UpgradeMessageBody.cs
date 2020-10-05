// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    /// <summary>
    /// The upgrade stream uses the raw connection stream instead of going through the RequestBodyPipe. This
    /// removes the redundant copy from the transport pipe to the body pipe.
    /// </summary>
    internal sealed class Http1UpgradeMessageBody : Http1MessageBody
    {
        public Http1UpgradeMessageBody(Http1Connection context, bool keepAlive)
            : base(context, keepAlive)
        {
            RequestUpgrade = true;
        }

        // This returns IsEmpty so we can avoid draining the body (since it's basically an endless stream)
        public override bool IsEmpty => true;

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            _context.Input.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            _context.Input.CancelPendingRead();
        }

        public override Task ConsumeAsync()
        {
            return Task.CompletedTask;
        }

        public override ValueTask StopAsync()
        {
            return default;
        }

        public override bool TryReadInternal(out ReadResult readResult)
        {
            return _context.Input.TryRead(out readResult);
        }

        public override ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default)
        {
            return _context.Input.ReadAsync(cancellationToken);
        }
    }
}
