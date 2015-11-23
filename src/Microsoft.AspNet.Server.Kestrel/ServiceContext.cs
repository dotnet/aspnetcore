// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Filter;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class ServiceContext
    {
        private IKestrelTrace _log;

        public ServiceContext()
        {
        }

        public ServiceContext(ServiceContext context)
        {
            AppLifetime = context.AppLifetime;
            _log = context.Log;
            ThreadPoolActions = context.ThreadPoolActions;
            FrameFactory = context.FrameFactory;
            DateHeaderValueManager = context.DateHeaderValueManager;
            ConnectionFilter = context.ConnectionFilter;
            NoDelay = context.NoDelay;
        }

        public IApplicationLifetime AppLifetime { get; set; }

        public IKestrelTrace Log
        {
            get
            {
                return _log;
            }
            set
            {
                _log = value;
                ThreadPoolActions = new ThreadPoolActions(_log);
            }
        }

        public ThreadPoolActions ThreadPoolActions { get; private set; }

        public Func<ConnectionContext, IPEndPoint, IPEndPoint, Action<IFeatureCollection>, Frame> FrameFactory { get; set; }

        public DateHeaderValueManager DateHeaderValueManager { get; set; }

        public IConnectionFilter ConnectionFilter { get; set; }

        public bool NoDelay { get; set; }
    }
}
