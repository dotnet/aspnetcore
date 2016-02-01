// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    class FrameResponseStream : Stream
    {
        private FrameContext _context;
        private FrameStreamState _state;

        public FrameResponseStream()
        {
            _state = FrameStreamState.Closed;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position { get; set; }

        public override void Flush()
        {
            ValidateState(default(CancellationToken));

            _context.FrameControl.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            var task = ValidateState(cancellationToken);
            if (task == null)
            {
                return _context.FrameControl.FlushAsync(cancellationToken);
            }
            return task;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateState(default(CancellationToken));

            _context.FrameControl.Write(new ArraySegment<byte>(buffer, offset, count));
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var task = ValidateState(cancellationToken);
            if (task == null)
            {
                return _context.FrameControl.WriteAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken);
            }
            return task;
        }

        public Stream StartAcceptingWrites()
        {
            // Only start if not aborted
            if (_state == FrameStreamState.Closed)
            {
                _state = FrameStreamState.Open;
            }

            return this;
        }

        public void PauseAcceptingWrites()
        {
            _state = FrameStreamState.Closed;
        }

        public void ResumeAcceptingWrites()
        {
            if (_state == FrameStreamState.Closed)
            {
                _state = FrameStreamState.Open;
            }
        }

        public void StopAcceptingWrites()
        {
            // Can't use dispose (or close) as can be disposed too early by user code
            // As exampled in EngineTests.ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes
            _state = FrameStreamState.Closed;
        }

        public void Abort()
        {
            // We don't want to throw an ODE until the app func actually completes.
            if (_state != FrameStreamState.Closed)
            {
                _state = FrameStreamState.Aborted;
            }
        }

        public void Initialize(FrameContext context)
        {
            _context = context;
        }

        public void Uninitialize()
        {
            _context = null;
            _state = FrameStreamState.Closed;
        }

        private Task ValidateState(CancellationToken cancellationToken)
        {
            switch (_state)
            {
                case FrameStreamState.Open:
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return TaskUtilities.GetCancelledTask(cancellationToken);
                    }
                    break;
                case FrameStreamState.Closed:
                    throw new ObjectDisposedException(nameof(FrameResponseStream));
                case FrameStreamState.Aborted:
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // Aborted state only throws on write if cancellationToken requests it
                        return TaskUtilities.GetCancelledTask(cancellationToken);
                    }
                    break;
            }
            return null;
        }
    }
}
