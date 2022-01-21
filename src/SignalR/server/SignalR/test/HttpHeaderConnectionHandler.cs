// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class HttpHeaderConnectionHandler : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var result = await connection.Transport.Input.ReadAsync();
        var buffer = result.Buffer;

        try
        {
            var headers = connection.Features.Get<IHttpContextFeature>().HttpContext.Request.Headers;

            var headerName = Encoding.UTF8.GetString(buffer.ToArray());
            var headerValues = headers.FirstOrDefault(h => string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase)).Value.ToArray();

            var data = Encoding.UTF8.GetBytes(string.Join(",", headerValues));

            await connection.Transport.Output.WriteAsync(data);
        }
        finally
        {
            connection.Transport.Input.AdvanceTo(buffer.End);
        }
    }
}
