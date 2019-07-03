// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class BodyControlTests
    {
        private delegate void AbortAction<TException>(BodyControl bodyControl, TException exception) where TException : Exception;

        private readonly static AbortAction<ConnectionAbortedException> abortConnectionAbortedException = (bc, e) => bc.Abort(e);
        private readonly static AbortAction<IOException> abortIOException = (bc, e) => bc.Abort(e);

        [Fact]
        public async Task BodyControlThrowAfterAbort_ConnectionAbortedException()
        {
            var ex = new ConnectionAbortedException("My error");

            await BodyControlThrowAfterAbort(ex, abortConnectionAbortedException);
        }

        [Fact]
        public async Task BodyControlThrowAfterAbort_IOException()
        {
            var ex = new IOException("My error");

            await BodyControlThrowAfterAbort(ex, abortIOException);
        }

        private static async Task BodyControlThrowAfterAbort<TException>(TException exception, AbortAction<TException> abortAction)
            where TException : Exception
        {
            var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());
            var (request, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody());

            abortAction(bodyControl, exception);

            await response.WriteAsync(new byte[1], 0, 1);
            Assert.Same(exception,
                (await Assert.ThrowsAsync<TException>(() => request.ReadAsync(new byte[1], 0, 1))).InnerException);
            Assert.Same(exception,
                (await Assert.ThrowsAsync<TException>(async () => await requestPipe.ReadAsync())).InnerException);
        }

        [Fact]
        public async Task BodyControlThrowOnAbortAfterUpgrade_ConnectionAbortedException()
        {
            var ex = new ConnectionAbortedException("My error");

            await BodyControlThrowOnAbortAfterUpgrade(ex, abortConnectionAbortedException);
        }

        [Fact]
        public async Task BodyControlThrowOnAbortAfterUpgrade_IOException()
        {
            var ex = new IOException("My error");

            await BodyControlThrowOnAbortAfterUpgrade(ex, abortIOException);
        }

        private async Task BodyControlThrowOnAbortAfterUpgrade<TException>(TException exception, AbortAction<TException> abortAction)
            where TException : Exception
        {
            var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());
            var (request, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody(upgradeable: true));

            var upgrade = bodyControl.Upgrade();
            abortAction(bodyControl, exception);

            var writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => response.WriteAsync(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, writeEx.Message);

            Assert.Same(exception,
                (await Assert.ThrowsAsync<TException>(() => request.ReadAsync(new byte[1], 0, 1))).InnerException);

            Assert.Same(exception,
                (await Assert.ThrowsAsync<TException>(() => upgrade.ReadAsync(new byte[1], 0, 1))).InnerException);

            Assert.Same(exception,
                (await Assert.ThrowsAsync<TException>(async () => await requestPipe.ReadAsync())).InnerException);

            await upgrade.WriteAsync(new byte[1], 0, 1);
        }

        [Fact]
        public async Task BodyControlThrowOnUpgradeAfterAbort_ConnectionAbortedException()
        {
            var ex = new ConnectionAbortedException("My error");

            await BodyControlThrowOnUpgradeAfterAbort(ex, abortConnectionAbortedException);
        }

        [Fact]
        public async Task BodyControlThrowOnUpgradeAfterAbort_IOException()
        {
            var ex = new IOException("My error");

            await BodyControlThrowOnUpgradeAfterAbort(ex, abortIOException);
        }

        private async Task BodyControlThrowOnUpgradeAfterAbort<TException>(TException exception, AbortAction<TException> abortAction)
            where TException : Exception
        {
            var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());

            var (request, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody(upgradeable: true));
            abortAction(bodyControl, exception);

            var upgrade = bodyControl.Upgrade();

            var writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => response.WriteAsync(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, writeEx.Message);

            Assert.Same(exception,
                (await Assert.ThrowsAsync<TException>(() => request.ReadAsync(new byte[1], 0, 1))).InnerException);

            Assert.Same(exception,
                (await Assert.ThrowsAsync<TException>(() => upgrade.ReadAsync(new byte[1], 0, 1))).InnerException);
            Assert.Same(exception,
                (await Assert.ThrowsAsync<TException>(async () => await requestPipe.ReadAsync())).InnerException);

            await upgrade.WriteAsync(new byte[1], 0, 1);
        }


        [Fact]
        public async Task RequestPipeMethodsThrowAfterAbort_ConnectionAbortedException()
        {
            var ex = new ConnectionAbortedException("My error");

            await RequestPipeMethodsThrowAfterAbort(ex, abortConnectionAbortedException);
        }

        [Fact]
        public async Task RequestPipeMethodsThrowAfterAbort_IOException()
        {
            var ex = new IOException("My error");

            await RequestPipeMethodsThrowAfterAbort(ex, abortIOException);
        }

        private async Task RequestPipeMethodsThrowAfterAbort<TException>(TException exception, AbortAction<TException> abortAction)
            where TException : Exception
        {
            var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());

            var (_, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody(upgradeable: true));
            abortAction(bodyControl, exception);

            await response.WriteAsync(new byte[1], 0, 1);
            Assert.Same(exception,
                (Assert.Throws<TException>(() => requestPipe.AdvanceTo(new SequencePosition()))).InnerException);
            Assert.Same(exception,
                (Assert.Throws<TException>(() => requestPipe.AdvanceTo(new SequencePosition(), new SequencePosition()))).InnerException);
            Assert.Same(exception,
                (Assert.Throws<TException>(() => requestPipe.CancelPendingRead())).InnerException);
            Assert.Same(exception,
                (Assert.Throws<TException>(() => requestPipe.TryRead(out var res))).InnerException);
            Assert.Same(exception,
                (Assert.Throws<TException>(() => requestPipe.Complete())).InnerException);
            Assert.Same(exception,
                (Assert.Throws<TException>(() => requestPipe.OnWriterCompleted(null, null))).InnerException);
        }

        [Fact]
        public async Task RequestPipeThrowsObjectDisposedExceptionAfterStop()
        {
            var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());

            var (_, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody());

            await bodyControl.StopAsync();

            Assert.Throws<ObjectDisposedException>(() => requestPipe.AdvanceTo(new SequencePosition()));
            Assert.Throws<ObjectDisposedException>(() => requestPipe.AdvanceTo(new SequencePosition(), new SequencePosition()));
            Assert.Throws<ObjectDisposedException>(() => requestPipe.CancelPendingRead());
            Assert.Throws<ObjectDisposedException>(() => requestPipe.TryRead(out var res));
            Assert.Throws<ObjectDisposedException>(() => requestPipe.Complete());
            Assert.Throws<ObjectDisposedException>(() => requestPipe.OnWriterCompleted(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await requestPipe.ReadAsync());
        }

        [Fact]
        public async Task ResponsePipeThrowsObjectDisposedExceptionAfterStop()
        {
            var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());

            var (_, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody());

            await bodyControl.StopAsync();

            Assert.Throws<ObjectDisposedException>(() => responsePipe.Advance(1));
            Assert.Throws<ObjectDisposedException>(() => responsePipe.CancelPendingFlush());
            Assert.Throws<ObjectDisposedException>(() => responsePipe.GetMemory());
            Assert.Throws<ObjectDisposedException>(() => responsePipe.GetSpan());
            Assert.Throws<ObjectDisposedException>(() => responsePipe.Complete());
            Assert.Throws<ObjectDisposedException>(() => responsePipe.OnReaderCompleted(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await responsePipe.WriteAsync(new Memory<byte>()));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await responsePipe.FlushAsync());
        }

        private class MockMessageBody : MessageBody
        {
            public MockMessageBody(bool upgradeable = false)
                : base(null)
            {
                RequestUpgrade = upgradeable;
            }

            public override void AdvanceTo(SequencePosition consumed)
            {
                throw new NotImplementedException();
            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {
                throw new NotImplementedException();
            }

            public override void CancelPendingRead()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception)
            {
                throw new NotImplementedException();
            }

            public override void OnWriterCompleted(Action<Exception, object> callback, object state)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public override bool TryRead(out ReadResult readResult)
            {
                throw new NotImplementedException();
            }
        }
    }
}
