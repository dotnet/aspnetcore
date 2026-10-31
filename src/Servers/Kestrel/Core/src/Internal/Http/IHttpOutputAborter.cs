// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal interface IHttpOutputAborter
{
    void Abort(ConnectionAbortedException abortReason, ConnectionEndReason reason);
    void OnInputOrOutputCompleted();
}
