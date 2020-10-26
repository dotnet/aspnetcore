// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    internal abstract class ApplicationWrapper
    {
        internal abstract object CreateContext(IFeatureCollection features);

        internal abstract Task ProcessRequestAsync(object context);

        internal abstract void DisposeContext(object context, Exception exception);
    }

    internal class ApplicationWrapper<TContext> : ApplicationWrapper, IHttpApplication<TContext>
    {
        private readonly IHttpApplication<TContext> _application;
        private readonly Action _preProcessRequestAsync;

        public ApplicationWrapper(IHttpApplication<TContext> application, Action preProcessRequestAsync)
        {
            _application = application;
            _preProcessRequestAsync = preProcessRequestAsync;
        }

        internal override object CreateContext(IFeatureCollection features)
        {
            return ((IHttpApplication<TContext>)this).CreateContext(features);
        }

        TContext IHttpApplication<TContext>.CreateContext(IFeatureCollection features)
        {
            return _application.CreateContext(features);
        }

        internal override void DisposeContext(object context, Exception exception)
        {
            ((IHttpApplication<TContext>)this).DisposeContext((TContext)context, exception);
        }

        void IHttpApplication<TContext>.DisposeContext(TContext context, Exception exception)
        {
            _application.DisposeContext(context, exception);
        }

        internal override Task ProcessRequestAsync(object context)
        {
            return ((IHttpApplication<TContext>)this).ProcessRequestAsync((TContext)context);
        }

        Task IHttpApplication<TContext>.ProcessRequestAsync(TContext context)
        {
            _preProcessRequestAsync();
            return _application.ProcessRequestAsync(context);
        }
    }
}
