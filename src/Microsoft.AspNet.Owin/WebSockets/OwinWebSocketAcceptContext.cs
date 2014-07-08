// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.Owin
{
    public class OwinWebSocketAcceptContext : IWebSocketAcceptContext
    {
        private IDictionary<string, object> _options = new Dictionary<string, object>(1);

	    public OwinWebSocketAcceptContext()
	    {
        }

        public string SubProtocol
        {
            get
            {
                object obj;
                if (_options.TryGetValue(OwinConstants.WebSocket.SubProtocol, out obj))
                {
                    return (string)obj;
                }
                return null;
            }
            set
            {
                _options[OwinConstants.WebSocket.SubProtocol] = value;
            }
        }

        public IDictionary<string, object> Options
        {
            get { return _options; }
        }
    }
}