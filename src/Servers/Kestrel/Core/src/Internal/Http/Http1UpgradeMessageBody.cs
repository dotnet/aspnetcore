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
        private int _userCanceled;

        public Http1UpgradeMessageBody(Http1Connection context, bool keepAlive)
            : base(context, keepAlive)
        {
            RequestUpgrade = true;
        }

        // This returns IsEmpty so we can avoid draining the body (since it's basically an endless stream)
        public override bool IsEmpty => true;

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfReaderCompleted();
            return ReadAsyncInternal(cancellationToken);
        }

        public override bool TryRead(out ReadResult result)
        {
            ThrowIfReaderCompleted();
            return TryReadInternal(out result);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            _context.Input.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            Interlocked.Exchange(ref _userCanceled, 1);
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
            // Ignore the canceled readResult unless it was canceled by the user.
            do
            {
                if (!_context.Input.TryRead(out readResult))
                {
                    return false;
                }
            } while (readResult.IsCanceled && Interlocked.Exchange(ref _userCanceled, 0) == 0);

            return true;
        }

        public override ValueTask<ReadResult> ReadAtLeastAsyncInternal(int minimumSize, CancellationToken cancellationToken = default)
        {
            ReadResult readResult;

            // Ignore the canceled readResult unless it was canceled by the user.
            do
            {
                var readTask = _context.Input.ReadAtLeastAsync(minimumSize, cancellationToken);

                if (!readTask.IsCompletedSuccessfully)
                {
                    return Awaited(this, readTask, minimumSize, cancellationToken);
                }

                readResult = readTask.GetAwaiter().GetResult();
            } while (readResult.IsCanceled && Interlocked.Exchange(ref _userCanceled, 0) == 0);

            return new ValueTask<ReadResult>(readResult);

            static async ValueTask<ReadResult> Awaited(Http1UpgradeMessageBody body, ValueTask<ReadResult> readTask, int minimumSize, CancellationToken cancellationToken = default)
            {
                var readResult = await readTask;

                // Ignore the canceled readResult unless it was canceled by the user.
                while (readResult.IsCanceled && Interlocked.Exchange(ref body._userCanceled, 0) == 0)
                {
                    readResult = await body._context.Input.ReadAtLeastAsync(minimumSize, cancellationToken);
                }

                return readResult;
            }
        }

        public override ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default)
        {
            ReadResult readResult;

            // Ignore the canceled readResult unless it was canceled by the user.
            do
            {
                var readTask = _context.Input.ReadAsync(cancellationToken);

                if (!readTask.IsCompletedSuccessfully)
                {
                    return Awaited(this, readTask, cancellationToken);
                }

                readResult = readTask.GetAwaiter().GetResult();
            } while (readResult.IsCanceled && Interlocked.Exchange(ref _userCanceled, 0) == 0);

            return new ValueTask<ReadResult>(readResult);

            static async ValueTask<ReadResult> Awaited(Http1UpgradeMessageBody body, ValueTask<ReadResult> readTask, CancellationToken cancellationToken = default)
            {
                var readResult = await readTask;

                // Ignore the canceled readResult unless it was canceled by the user.
                while (readResult.IsCanceled && Interlocked.Exchange(ref body._userCanceled, 0) == 0)
                {
                    readResult = await body._context.Input.ReadAsync(cancellationToken);
                }

                return readResult;
            }
        }
    }
}
