// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using MessagePack;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public class MessagePackHubProtocolOptions
    {
        private IList<IFormatterResolver> _formatterResolvers;

        public IList<IFormatterResolver> FormatterResolvers
        {
            get
            {
                if (_formatterResolvers == null)
                {
                    _formatterResolvers = MessagePackHubProtocol.CreateDefaultFormatterResolvers();
                }

                return _formatterResolvers;
            }
            set
            {
                _formatterResolvers = value;
            }
        }
    }
}
