// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DiagnosticAdapter;

namespace Microsoft.AspNetCore.Diagnostics;

public class TestDiagnosticListener
{
    public class OnRequestEventData
    {
        public IProxyHttpContext HttpContext { get; set; }
    }

    public class OnExceptionEventData
    {
        public IProxyHttpContext HttpContext { get; set; }
        public IProxyException Exception { get; set; }
    }

    public OnRequestEventData BeginRequest { get; set; }
    public OnRequestEventData EndRequest { get; set; }
    public OnExceptionEventData HostingUnhandledException { get; set; }
    public OnExceptionEventData DiagnosticUnhandledException { get; set; }
    public OnExceptionEventData DiagnosticHandledException { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Hosting.BeginRequest")]
    public virtual void OnBeginRequest(IProxyHttpContext httpContext)
    {
        BeginRequest = new OnRequestEventData()
        {
            HttpContext = httpContext
        };
    }

    [DiagnosticName("Microsoft.AspNetCore.Hosting.EndRequest")]
    public virtual void OnEndRequest(IProxyHttpContext httpContext)
    {
        EndRequest = new OnRequestEventData()
        {
            HttpContext = httpContext
        };
    }

    [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
    public virtual void OnHostingUnhandledException(IProxyHttpContext httpContext, IProxyException exception)
    {
        HostingUnhandledException = new OnExceptionEventData()
        {
            HttpContext = httpContext,
            Exception = exception
        };
    }

    [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
    public virtual void OnDiagnosticUnhandledException(IProxyHttpContext httpContext, IProxyException exception)
    {
        DiagnosticUnhandledException = new OnExceptionEventData()
        {
            HttpContext = httpContext,
            Exception = exception
        };
    }

    [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
    public virtual void OnDiagnosticHandledException(IProxyHttpContext httpContext, IProxyException exception)
    {
        DiagnosticHandledException = new OnExceptionEventData()
        {
            HttpContext = httpContext,
            Exception = exception
        };
    }

    public interface IProxyHttpContext
    {
    }

    public interface IProxyException
    {
    }
}
