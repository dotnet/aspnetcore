// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost;

internal abstract class ApplicationWrapper
{
    internal abstract object CreateContext(IFeatureCollection features);

    internal abstract Task ProcessRequestAsync(object context);

    internal abstract void DisposeContext(object context, Exception? exception);
}

internal sealed class ApplicationWrapper<TContext> : ApplicationWrapper, IHttpApplication<TContext> where TContext : notnull
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

    internal override void DisposeContext(object context, Exception? exception)
    {
        ((IHttpApplication<TContext>)this).DisposeContext((TContext)context, exception);
    }

    void IHttpApplication<TContext>.DisposeContext(TContext context, Exception? exception)
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
