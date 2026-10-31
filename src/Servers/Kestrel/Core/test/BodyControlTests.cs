// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class BodyControlTests
{
    [Fact]
    public async Task BodyControlThrowAfterAbort()
    {
        var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());
        var (request, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody());

        var ex = new Exception("My error");
        bodyControl.Abort(ex);

        await response.WriteAsync(new byte[1], 0, 1);
        Assert.Same(ex,
            await Assert.ThrowsAsync<Exception>(() => request.ReadAsync(new byte[1], 0, 1)));
        Assert.Same(ex,
            await Assert.ThrowsAsync<Exception>(async () => await requestPipe.ReadAsync()));
    }

    [Fact]
    public async Task BodyControlThrowOnAbortAfterUpgrade()
    {
        var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());
        var (request, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody(upgradeable: true));

        var upgrade = bodyControl.Upgrade();
        var ex = new Exception("My error");
        bodyControl.Abort(ex);

        var writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => response.WriteAsync(new byte[1], 0, 1));
        Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, writeEx.Message);

        Assert.Same(ex,
            await Assert.ThrowsAsync<Exception>(() => request.ReadAsync(new byte[1], 0, 1)));

        Assert.Same(ex,
            await Assert.ThrowsAsync<Exception>(() => upgrade.ReadAsync(new byte[1], 0, 1)));

        Assert.Same(ex,
            await Assert.ThrowsAsync<Exception>(async () => await requestPipe.ReadAsync()));

        await upgrade.WriteAsync(new byte[1], 0, 1);
    }

    [Fact]
    public async Task BodyControlThrowOnUpgradeAfterAbort()
    {
        var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());

        var (request, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody(upgradeable: true));
        var ex = new Exception("My error");
        bodyControl.Abort(ex);

        var upgrade = bodyControl.Upgrade();

        var writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => response.WriteAsync(new byte[1], 0, 1));
        Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, writeEx.Message);

        Assert.Same(ex,
            await Assert.ThrowsAsync<Exception>(() => request.ReadAsync(new byte[1], 0, 1)));

        Assert.Same(ex,
            await Assert.ThrowsAsync<Exception>(() => upgrade.ReadAsync(new byte[1], 0, 1)));
        Assert.Same(ex,
            await Assert.ThrowsAsync<Exception>(async () => await requestPipe.ReadAsync()));

        await upgrade.WriteAsync(new byte[1], 0, 1);
    }

    [Fact]
    public async Task RequestPipeMethodsThrowAfterAbort()
    {
        var bodyControl = new BodyControl(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());

        var (_, response, requestPipe, responsePipe) = bodyControl.Start(new MockMessageBody(upgradeable: true));
        var ex = new Exception("My error");
        bodyControl.Abort(ex);

        await response.WriteAsync(new byte[1], 0, 1);
        Assert.Same(ex,
            Assert.Throws<Exception>(() => requestPipe.AdvanceTo(new SequencePosition())));
        Assert.Same(ex,
            Assert.Throws<Exception>(() => requestPipe.AdvanceTo(new SequencePosition(), new SequencePosition())));
        Assert.Same(ex,
            Assert.Throws<Exception>(() => requestPipe.CancelPendingRead()));
        Assert.Same(ex,
            Assert.Throws<Exception>(() => requestPipe.TryRead(out var res)));
        Assert.Same(ex,
            Assert.Throws<Exception>(() => requestPipe.Complete()));
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
