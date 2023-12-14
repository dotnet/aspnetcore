// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class Streams
{
    private static readonly ThrowingWasUpgradedWriteOnlyStreamInternal _throwingResponseStream
        = new ThrowingWasUpgradedWriteOnlyStreamInternal();

    private readonly IISHttpContext _context;
    private readonly HttpResponseStream _response;
    private readonly HttpRequestStream _request;
    private readonly WrappingStream _upgradeableRequest;
    private readonly WrappingStream _upgradeableResponse;
    private EmptyStream? _emptyRequest;
    private Stream? _upgradeStream;

    public Streams(IISHttpContext context)
    {
        _context = context;
        _request = new HttpRequestStream(_context);
        _response = new HttpResponseStream(_context, _context);
        _upgradeableResponse = new WrappingStream(_response);
        _upgradeableRequest = new WrappingStream(_request);
    }

    public Stream Upgrade()
    {
        _upgradeStream = new HttpUpgradeStream(_request, _response);

        // causes writes to context.Response.Body to throw
        _upgradeableResponse.SetInnerStream(_throwingResponseStream);

        _emptyRequest = new EmptyStream(_context);

        _upgradeableRequest.SetInnerStream(_emptyRequest);
        // _upgradeStream always uses _response
        return _upgradeStream;
    }

    public (Stream request, Stream response) Start()
    {
        _request.StartAcceptingReads(_context);
        _response.StartAcceptingWrites();

        return (_upgradeableRequest, _upgradeableResponse);
    }

    public void Stop()
    {
        _request.StopAcceptingReads();
        _emptyRequest?.StopAcceptingReads();
        _response.StopAcceptingWrites();
    }

    public void Abort(Exception error)
    {
        _request.Abort(error);
        _emptyRequest?.Abort(error);
        _response.Abort();
    }
}
