// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.InternalTesting;

internal sealed class TestBodyControlFeature : IHttpBodyControlFeature
{
    public bool AllowSynchronousIO { get; set; }
}
