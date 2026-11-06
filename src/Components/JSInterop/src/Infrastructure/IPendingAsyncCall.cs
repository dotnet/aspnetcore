// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

internal interface IPendingAsyncCall
{
    void Complete(JSRuntime runtime, ref Utf8JsonReader reader);

    void Fail(Exception exception);

    void Cancel(CancellationToken cancellationToken);
}
