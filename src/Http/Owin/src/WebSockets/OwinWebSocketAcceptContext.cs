// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Owin;

/// <summary>
/// OWIN WebSocket accept context.
/// </summary>
public class OwinWebSocketAcceptContext : WebSocketAcceptContext
{
    private IDictionary<string, object> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="OwinWebSocketAcceptContext"/>.
    /// </summary>
    public OwinWebSocketAcceptContext() : this(new Dictionary<string, object>(1))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OwinWebSocketAcceptContext"/>.
    /// </summary>
    /// <param name="options">OWIN WebSocket options.</param>
    public OwinWebSocketAcceptContext(IDictionary<string, object> options)
    {
        _options = options;
    }

    /// <inheritdocs />
    public override string SubProtocol
    {
        get
        {
            object obj;
            if (_options != null && _options.TryGetValue(OwinConstants.WebSocket.SubProtocol, out obj))
            {
                return (string)obj;
            }
            return null;
        }
        set
        {
            if (_options == null)
            {
                _options = new Dictionary<string, object>(1);
            }
            _options[OwinConstants.WebSocket.SubProtocol] = value;
        }
    }

    /// <summary>
    /// Gets OWIN WebSocket options.
    /// </summary>
    public IDictionary<string, object> Options
    {
        get { return _options; }
    }
}
