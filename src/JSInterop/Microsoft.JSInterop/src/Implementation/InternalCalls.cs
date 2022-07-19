// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.JSInterop.Implementation;

internal interface IJSInternalCalls : IInternalCalls
{
    void DisposeJSObjectReferenceById([JSMarshalAs<JSType.Number>] long id);
}

internal partial class DefaultInternalCalls : IJSInternalCalls
{
    internal static readonly IInternalCalls Instance = new DefaultInternalCalls();

    public void DisposeJSObjectReferenceById([JSMarshalAs<JSType.Number>] long id) => _DisposeJSObjectReferenceById(id);

    [JSImport("DotNet.jsCallDispatcher.disposeJSObjectReferenceById")]
    private static partial void _DisposeJSObjectReferenceById([JSMarshalAs<JSType.Number>] long id);
}
